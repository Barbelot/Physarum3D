# Physarum3D

Unity implementation of the [Physarum Transport Network](https://www.mitpressjournals.org/doi/abs/10.1162/artl.2010.16.2.16202) from Jeff Jones in 3D.

[Result Video](https://vimeo.com/379589358)

![Result Image](https://benoitarbelot.files.wordpress.com/2020/01/physarum3d.png)

# Disclaimer

This project is a prototype and work in progress. It is not actively maintained and may (or may not) evolve in the future.

# Example project with no setup

https://github.com/miketucker/Physarum3D

# Manual Setup

- Download this repo or clone it inside a folder in your Unity project.
- Add the PhysarumVolumeController.cs script to an object in your scene.
- Assign the PhysarumVolume compute shader and particle position/color/velocity textures to the PhysarumVolumeController script.
- Create a VFX Graph and with the SetPositionFromMap and SetColorFromMap blocks in the Update context.
- Set ParticlePositionMap and ParticleColorMap in those blocks and switch their sampling mode to Sequential.

# Technical Details

The particles positions and trail volume are computed with compute shaders.

The particles are displayed using Unity Visual Effect Graph and the HDRP pipeline.
The trail volume can be displayed through volumetric rendering using the VolumeRayCast shader.

Tested with Unity 2019.3.0f1 and the corresponding HDRP/VFX Graph packages.

# Acknowledgments

- [Sage Jenson](https://sagejenson.com/) for the helpful discussions
- DenizBicer for the [2D implementation](https://github.com/DenizBicer/Physarum) in Unity
- Scrawk for the [GPU-GEMS-3D-Fluid-Simulation](https://github.com/Scrawk/GPU-GEMS-3D-Fluid-Simulation)
