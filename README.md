RVO2-Unity
===

A Unity package forked from warmtree/rvo2-unity, which implements the UNC RVO2 paper found at https://github/snape/RVO2-CS.

Apart from styling, the code is unchanged from warmtree's implementation. The main difference is the reordering of the folder structure to comply with the Unity package structure, and the creation of some components:
* **RVOAgent** is applied to anything that uses collision avoidance. Currently the simlation can steer the transform directly, or it returns a Vector3 that can be used by other components.
* **RVOManager** is a wrapper for the Simulation singleton. Currently it allows the user to set simlation defaults (which can be ignored by individual agents), and set the 2D frame of reference (default XY). You will need one in the scene to handle collision avoidance
* **RVOObstacle** is coming soon!

## Installation
Simply add "https://github.com/grahamegw/rvo2-unity.git" to the "Add from git" option in the Unity Package Manager

![screenshot](https://warmtrue-1253180525.cos.ap-beijing.myqcloud.com/2.gif)

## Feature
* add dynamic add/del agent in runtime.
* add ObstacleCollect to convert BoxCollider to RVO Obstacle
* add find near agent API queryNearAgent
* simple example for use in unity


## Requirement
* Unity 2017.1.2 (simulation); package developed on Unity 2021.3.3;
* No other SDK are required.


## Author
[warmtrue](http://github.com/warmtrue/RVO2-Unity)
