# Kazimir

**EPFL | Media & Design Lab | Master Project**

**Immanuel Koh (Supervisor)**

**Jeffrey Huang (Professor)**

Unity project that creates 3D voxel models that are similar to the input 3D voxel model, based on Model Synthesis and the WFC algorithm.

The long-term goal of the project is to try and completely automatise the creation of 3D models that is costly and time-consuming. Using this project, the user can give a relatively small 3D model as an input and the algorithm will try and generate a larger 3D model that is consistent with the input.

This project presents two different models:

1. **The Simple Model**: cuts up the input model into NxNxN unique patterns and then tries to infer the adjacency rules based on their positions in the input model.
<p align="center"><img src="http://imgur.com/DPTYsSQ.png"></p>
<p align="center"><img alt="main city2" src="http://imgur.com/fLVaeQU.png"></p>

2. **The Convolutional Model**: cuts up the input model into NxNxN unique patterns that overlap with each other and then tries to convolute all the possible pairs of unique patterns in order to define the adjacency rules. The reulst of this approach is that the algorithm can generate patterns that are completely new and not just the patterns seen in the input. Although this model is much slower and only works on small inputs (for now at least).
<p align="center"><img alt="main hollowcube" src="http://imgur.com/OTuwnhZ.png"></p>
<p align="center"><img alt="main magicube" src="http://imgur.com/Uf91sEA.png"></p>

## Instructions
Clone the repo and open the project in the Unity editor. In the `Assets/Scripts/ModSynth` folder, we have `SimpleModel.cs` and `ConvolutionalModel.cs` scripts which define the logic of the models and we have `SimpleModelDemo.cs` and `ConvolutionalModelDemo.cs` which we can attach to empty game pbjects in Unity. The flags which the user can set are:

1. `Vox File Name`: the name of the [MagicaVoxel](https://ephtracy.github.io/) `.vox` input file (currently the only supported input format).
2. `Probabilistic Model`: defines if the programme should use the pattern distribution in the input voxel model in order to choose the pattern that is chosen in the observation step. If the flag is set to false, the pattern is randomly chosen.
3. `Periodic`: defines if the input voxel model is periodic or not (i.e. if it can be "loope over" or not).
4. `Pattern Size`: defines the N for the pattern size (i.e. if pattern size is set to 3, then the patterns will be of size 3x3x3).
5. `Output Size`: defines the desired size of the produced output. Measured in patterns for the Simple Model and in voxels for the Convolutional Model (As of now should not be too big.)
6. `Out Vox File Name`: defines the name of the `.vox` file name to which the output can be written.
7. `Augment Neighbours`: only for the Simple Model. Tries to increase the amound of possible neighbours for each pattern by checking if two patterns can "fit" together for each possible direction.

Once all the input and all the flags are set, press play and you should be able to see the input model rendered in Unity. Now you have several actions available:

* Press `space` to generate an output model.
* Press `v` to visualize the output in Unity.
* Press `w` to write the output to a .vox file which you can then use in MagicaVoxel.
* Press `c` to clear the output.

Examples can be found in the `TestScene`, which can be found in `Assets/Scenes` folder.

Keep in mind that this project is still a **work in progress** and it is likely to be slow and not very stable.

## Based on work by

Based on Paul C. Merrell's [dissertation on Model Synthesis](http://graphics.stanford.edu/~pmerrell/thesis.pdf) and the [Wave Function Collapse algorithm](https://github.com/mxgmn/WaveFunctionCollapse) made by [mxgmn](https://github.com/mxgmn).
