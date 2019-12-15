# Physarum3D

Unity implementation of the [Physarum Transport Network](https://www.mitpressjournals.org/doi/abs/10.1162/artl.2010.16.2.16202) from Jeff Jones in 3D.

[Result Video](https://vimeo.com/379589358)


# Technical Details

The particles positions and trail volume are computed with compute shaders.

The particles are displayed using Unity Visual Effect Graph and the HDRP pipeline.
The trail volume can be displayed through volumetric rendering using the VolumeRayCast shader.

# Acknowledgments

- [Sage Jenson](https://sagejenson.com/) for the helpful discussions
- DenizBicer for the [2D implementation](https://github.com/DenizBicer/Physarum) in Unity
- Scrawk for the [GPU-GEMS-3D-Fluid-Simulation](https://github.com/Scrawk/GPU-GEMS-3D-Fluid-Simulation)
