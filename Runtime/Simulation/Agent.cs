/*
 * Agent.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using System.Collections.Generic;

namespace TiercelFoundry.RVO2
{
    /**
     * <summary>Defines an agent in the simulation.</summary>
     */
    internal class Agent
    {
        internal IList<KeyValuePair<float, Agent>> agentNeighbors_ = new List<KeyValuePair<float, Agent>>();
        internal IList<KeyValuePair<float, Obstacle>> obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();
        internal IList<Line> orcaLines_ = new List<Line>();
        internal Vector2 position_;
        internal Vector2 prefVelocity_;
        internal Vector2 velocity_;
        internal int id_ = 0;
        internal int maxNeighbors_ = 0;
        internal float maxSpeed_ = 0.0f;
        internal float neighborDist_ = 0.0f;
        internal float radius_ = 0.0f;
        internal float timeHorizon_ = 0.0f;
        internal float timeHorizonObst_ = 0.0f;
        internal bool needDelete_ = false;

        private Vector2 newVelocity_;

        /**
         * <summary>Computes the neighbors of this agent.</summary>
         */
        internal void ComputeNeighbors()
        {
            obstacleNeighbors_.Clear();
            float rangeSq = RVOMath.Sqr(timeHorizonObst_ * maxSpeed_ + radius_);
            Simulator.Instance.kdTree_.ComputeObstacleNeighbors(this, rangeSq);

            agentNeighbors_.Clear();

            if (maxNeighbors_ > 0)
            {
                rangeSq = RVOMath.Sqr(neighborDist_);
                Simulator.Instance.kdTree_.ComputeAgentNeighbors(this, ref rangeSq);
            }
        }

        /**
         * <summary>Computes the new velocity of this agent.</summary>
         */
        internal void ComputeNewVelocity()
        {
            orcaLines_.Clear();

            float invTimeHorizonObst = 1.0f / timeHorizonObst_;

            /* Create obstacle ORCA lines. */
            for (int i = 0; i < obstacleNeighbors_.Count; ++i)
            {

                Obstacle obstacle1 = obstacleNeighbors_[i].Value;
                Obstacle obstacle2 = obstacle1.next_;

                Vector2 relativePosition1 = obstacle1.point_ - position_;
                Vector2 relativePosition2 = obstacle2.point_ - position_;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                 */
                bool alreadyCovered = false;

                for (int j = 0; j < orcaLines_.Count; ++j)
                {
                    if (RVOMath.Det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON && RVOMath.Det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON)
                    {
                        alreadyCovered = true;

                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                /* Not yet covered. Check for collisions. */
                float distSq1 = RVOMath.AbsSq(relativePosition1);
                float distSq2 = RVOMath.AbsSq(relativePosition2);

                float radiusSq = RVOMath.Sqr(radius_);

                Vector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
                float s = (-relativePosition1 * obstacleVector) / RVOMath.AbsSq(obstacleVector);
                float distSqLine = RVOMath.AbsSq(-relativePosition1 - s * obstacleVector);

                Line line;

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1.convex_)
                    {
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.Normalize(new Vector2(-relativePosition1.Y(), relativePosition1.X()));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                    if (obstacle2.convex_ && RVOMath.Det(relativePosition2, obstacle2.direction_) >= 0.0f)
                    {
                        line.point = new Vector2(0.0f, 0.0f);
                        line.direction = RVOMath.Normalize(new Vector2(-relativePosition2.Y(), relativePosition2.X()));
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.point = new Vector2(0.0f, 0.0f);
                    line.direction = -obstacle1.direction_;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */

                Vector2 leftLegDirection, rightLegDirection;

                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    if (!obstacle1.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle2 = obstacle1;

                    float leg1 = RVOMath.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.X() * leg1 - relativePosition1.Y() * radius_, relativePosition1.X() * radius_ + relativePosition1.Y() * leg1) / distSq1;
                    rightLegDirection = new Vector2(relativePosition1.X() * leg1 + relativePosition1.Y() * radius_, -relativePosition1.X() * radius_ + relativePosition1.Y() * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    if (!obstacle2.convex_)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle1 = obstacle2;

                    float leg2 = RVOMath.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition2.X() * leg2 - relativePosition2.Y() * radius_, relativePosition2.X() * radius_ + relativePosition2.Y() * leg2) / distSq2;
                    rightLegDirection = new Vector2(relativePosition2.X() * leg2 + relativePosition2.Y() * radius_, -relativePosition2.X() * radius_ + relativePosition2.Y() * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1.convex_)
                    {
                        float leg1 = RVOMath.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.X() * leg1 - relativePosition1.Y() * radius_, relativePosition1.X() * radius_ + relativePosition1.Y() * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1.direction_;
                    }

                    if (obstacle2.convex_)
                    {
                        float leg2 = RVOMath.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector2(relativePosition2.X() * leg2 + relativePosition2.Y() * radius_, -relativePosition2.X() * radius_ + relativePosition2.Y() * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1.direction_;
                    }
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                Obstacle leftNeighbor = obstacle1.previous_;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.convex_ && RVOMath.Det(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.direction_;
                    isLeftLegForeign = true;
                }

                if (obstacle2.convex_ && RVOMath.Det(rightLegDirection, obstacle2.direction_) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.direction_;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
                Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
                Vector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                float t = obstacle1 == obstacle2 ? 0.5f : ((velocity_ - leftCutOff) * cutOffVector) / RVOMath.AbsSq(cutOffVector);
                float tLeft = (velocity_ - leftCutOff) * leftLegDirection;
                float tRight = (velocity_ - rightCutOff) * rightLegDirection;

                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector2 unitW = RVOMath.Normalize(velocity_ - leftCutOff);

                    line.direction = new Vector2(unitW.Y(), -unitW.X());
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector2 unitW = RVOMath.Normalize(velocity_ - rightCutOff);

                    line.direction = new Vector2(unitW.Y(), -unitW.X());
                    line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : RVOMath.AbsSq(velocity_ - (leftCutOff + t * cutOffVector));
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RVOMath.AbsSq(velocity_ - (leftCutOff + tLeft * leftLegDirection));
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RVOMath.AbsSq(velocity_ - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.direction = -obstacle1.direction_;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.Y(), line.direction.X());
                    orcaLines_.Add(line);

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.direction = leftLegDirection;
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.Y(), line.direction.X());
                    orcaLines_.Add(line);

                    continue;
                }

                /* Project on right leg. */
                if (isRightLegForeign)
                {
                    continue;
                }

                line.direction = -rightLegDirection;
                line.point = rightCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.Y(), line.direction.X());
                orcaLines_.Add(line);
            }

            int numObstLines = orcaLines_.Count;

            float invTimeHorizon = 1.0f / timeHorizon_;

            /* Create agent ORCA lines. */
            for (int i = 0; i < agentNeighbors_.Count; ++i)
            {
                Agent other = agentNeighbors_[i].Value;

                Vector2 relativePosition = other.position_ - position_;
                Vector2 relativeVelocity = velocity_ - other.velocity_;
                float distSq = RVOMath.AbsSq(relativePosition);
                float combinedRadius = radius_ + other.radius_;
                float combinedRadiusSq = RVOMath.Sqr(combinedRadius);

                Line line;
                Vector2 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = RVOMath.AbsSq(w);
                    float dotProduct1 = w * relativePosition;

                    if (dotProduct1 < 0.0f && RVOMath.Sqr(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = RVOMath.Sqrt(wLengthSq);
                        Vector2 unitW = w / wLength;

                        line.direction = new Vector2(unitW.Y(), -unitW.X());
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = RVOMath.Sqrt(distSq - combinedRadiusSq);

                        if (RVOMath.Det(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.direction = new Vector2(relativePosition.X() * leg - relativePosition.Y() * combinedRadius, relativePosition.X() * combinedRadius + relativePosition.Y() * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new Vector2(relativePosition.X() * leg + relativePosition.Y() * combinedRadius, -relativePosition.X() * combinedRadius + relativePosition.Y() * leg) / distSq;
                        }

                        float dotProduct2 = relativeVelocity * line.direction;
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    float invTimeStep = 1.0f / Simulator.Instance.timeStep_;

                    /* Vector from cutoff center to relative velocity. */
                    Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = RVOMath.Abs(w);
                    Vector2 unitW = w / wLength;

                    line.direction = new Vector2(unitW.Y(), -unitW.X());
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocity_ + 0.5f * u;
                orcaLines_.Add(line);
            }

            int lineFail = LinearProgramTwo(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);

            if (lineFail < orcaLines_.Count)
            {
                LinearProgramThree(orcaLines_, numObstLines, lineFail, maxSpeed_, ref newVelocity_);
            }
        }

        /**
         * <summary>Inserts an agent neighbor into the set of neighbors of this
         * agent.</summary>
         *
         * <param name="agent">A pointer to the agent to be inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void InsertAgentNeighbor(Agent agent, ref float rangeSq)
        {
            if (this != agent)
            {
                float distSq = RVOMath.AbsSq(position_ - agent.position_);

                if (distSq < rangeSq)
                {
                    if (agentNeighbors_.Count < maxNeighbors_)
                    {
                        agentNeighbors_.Add(new KeyValuePair<float, Agent>(distSq, agent));
                    }

                    int i = agentNeighbors_.Count - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key)
                    {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<float, Agent>(distSq, agent);

                    if (agentNeighbors_.Count == maxNeighbors_)
                    {
                        rangeSq = agentNeighbors_[agentNeighbors_.Count - 1].Key;
                    }
                }
            }
        }

        /**
         * <summary>Inserts a static obstacle neighbor into the set of neighbors
         * of this agent.</summary>
         *
         * <param name="obstacle">The number of the static obstacle to be
         * inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void InsertObstacleNeighbor(Obstacle obstacle, float rangeSq)
        {
            Obstacle nextObstacle = obstacle.next_;

            float distSq = RVOMath.DistSqPointLineSegment(obstacle.point_, nextObstacle.point_, position_);

            if (distSq < rangeSq)
            {
                obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

                int i = obstacleNeighbors_.Count - 1;

                while (i != 0 && distSq < obstacleNeighbors_[i - 1].Key)
                {
                    obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
                    --i;
                }
                obstacleNeighbors_[i] = new KeyValuePair<float, Obstacle>(distSq, obstacle);
            }
        }

        /**
         * <summary>Updates the two-dimensional position and two-dimensional
         * velocity of this agent.</summary>
         */
        internal void Update()
        {
            velocity_ = newVelocity_;
            position_ += velocity_ * Simulator.Instance.timeStep_;
        }

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool LinearProgramOne(IList<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            float dotProduct = lines[lineNo].point * lines[lineNo].direction;
            float discriminant = RVOMath.Sqr(dotProduct) + RVOMath.Sqr(radius) - RVOMath.AbsSq(lines[lineNo].point);

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = RVOMath.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = RVOMath.Det(lines[lineNo].direction, lines[i].direction);
                float numerator = RVOMath.Det(lines[i].direction, lines[lineNo].point - lines[i].point);

                if (RVOMath.AbsF(denominator) <= RVOMath.RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Math.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Math.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (optVelocity * lines[lineNo].direction > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = lines[lineNo].direction * (optVelocity - lines[lineNo].point);

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private int LinearProgramTwo(IList<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                result = optVelocity * radius;
            }
            else if (RVOMath.AbsSq(optVelocity) > RVOMath.Sqr(radius))
            {
                /* Optimize closest point and outside circle. */
                result = RVOMath.Normalize(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                if (RVOMath.Det(lines[i].direction, lines[i].point - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!LinearProgramOne(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="numObstLines">Count of obstacle lines.</param>
         * <param name="beginLine">The line on which the 2-d linear program
         * failed.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private void LinearProgramThree(IList<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                if (RVOMath.Det(lines[i].direction, lines[i].point - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    IList<Line> projLines = new List<Line>();
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        float determinant = RVOMath.Det(lines[i].direction, lines[j].direction);

                        if (RVOMath.AbsF(determinant) <= RVOMath.RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            if (lines[i].direction * lines[j].direction > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (RVOMath.Det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = RVOMath.Normalize(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (LinearProgramTwo(projLines, radius, new Vector2(-lines[i].direction.Y(), lines[i].direction.X()), true, ref result) < projLines.Count)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = RVOMath.Det(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}
