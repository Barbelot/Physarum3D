using System.Collections.Generic;
using UnityEngine;

public class PhysarumVolumeController : MonoBehaviour
{
	//DONT CHANGE THESE
	const int READ = 0;
	const int WRITE = 1;

    [Header("Initial Values")]
    [SerializeField] private int numberOfParticles = 10000;
    [SerializeField] private Vector3 size = new Vector3(64, 64, 64);
    [SerializeField] private ComputeShader shader;
	[SerializeField] public RenderTexture particlePositionMap;
	[SerializeField] public RenderTexture particleColorMap;
	[SerializeField] public RenderTexture particleVelocityMap;
	//[SerializeField] public MeshToSDF meshToSDF;

	[Header("Runtime Parameters")]
	public float updatesPerSecond = 60;
    [Range(0f, 1f)] public float trailDecay = 0.002f;
    public Vector2 sensorAngleDegrees = Vector2.one * 45f; 	//in degrees
    public Vector2 rotationAngleDegrees = Vector2.one * 45f;//in degrees
	[Range(0f, 1f)] public float randomRotationProbability = 0;
	[Range(0f, 10f)] public float sensorOffsetDistance = 0.01f;
    [Range(0f, 10f)] public float stepSize = 0.001f;
	public bool diffuseTrail = true;
	[Range(0f, 1f)] public float trailDiffuseSpeed = 1;
	[Range(0f, 1f)] public float trailRepulsion = 0;
	[Range(0f, 1f)] public float meshImportance = 1;
	public float trailToSizeImportance = 1;

	[Header("Debug")]
	public bool reinitialize = false;
	public bool useSensorRotationMaster = false;
	[Range(0f, 360f)] public float sensorRotationMaster = 12;

    private Vector2 sensorAngle; 				//in radians
    private Vector2 rotationAngle;            //in radians
	private int initParticlesKernel, updateParticlesKernel, initTrailKernel, updateTrailKernel, updateParticleMapKernel;
    private ComputeBuffer particleBuffer;
	private ComputeBuffer[] trailDensityBuffer;
	private RenderTexture _tmpParticlePositionMap;
	private RenderTexture _tmpParticleColorMap;
	private RenderTexture _tmpParticleVelocityMap;

    private static int groupCount3D = 8;       // Group size has to be same with the compute shader group size
	private static int groupCount1D = 512;

    struct Particle
    {
        public Vector3 position;
		public Vector2 angle;
		public Vector4 color;
		public Vector3 velocity;
		public float size;

		public Particle(Vector3 position, Vector2 angle, Vector4 color, Vector3 velocity, float size)
        {
            this.position = position;
			this.angle = angle;
			this.color = color;
			this.velocity = velocity;
			this.size = size;
        }
    };

	#region MonoBehaviour Functions

	void Start()
    {
        if (shader == null)
        {
            Debug.LogError("PhysarumSurface shader has to be assigned for PhysarumBehaviour to work.");
            this.enabled = false;
            return;
        }

        initParticlesKernel     = shader.FindKernel("InitParticles");
        updateParticlesKernel	= shader.FindKernel("UpdateParticles");
        initTrailKernel			= shader.FindKernel("InitTrail");
		updateTrailKernel		= shader.FindKernel("UpdateTrail");
		updateParticleMapKernel	= shader.FindKernel("UpdateParticleMap");

		Initialize();
	}

	void Update() {

		if (reinitialize) {
			Initialize();
			reinitialize = false;
		}

		int nbOfUpdatesNeeded = Mathf.CeilToInt(Time.deltaTime * updatesPerSecond);

		for (int i = 0; i < nbOfUpdatesNeeded; i++) {
			UpdateRuntimeParameters();
			UpdateParticles();
			UpdateTrail();
			UpdateParticleMap();
		}
	}

	void OnDestroy() {
		if (particleBuffer != null) particleBuffer.Release();
		if (trailDensityBuffer[READ] != null) trailDensityBuffer[READ].Release();
		if (trailDensityBuffer[WRITE] != null) trailDensityBuffer[WRITE].Release();
		if (_tmpParticlePositionMap != null) { Destroy(_tmpParticlePositionMap); _tmpParticlePositionMap = null; }
	}

	#endregion

	#region Initialization Functions

	void Initialize() {
		InitializeParticles();
		InitializeTrail();
		InitializeParticleMap();
	}

	void InitializeParticles()
    {
        // allocate memory for the particles
        if (numberOfParticles < groupCount1D) numberOfParticles = groupCount1D;

        Particle[] particleArray = new Particle[numberOfParticles];
        particleBuffer = new ComputeBuffer(particleArray.Length, 13*sizeof(float));
        particleBuffer.SetData(particleArray);

        //initialize particles with random positions
        shader.SetVector("_Size", size);
        shader.SetBuffer(initParticlesKernel, "_ParticleBuffer", particleBuffer);
        shader.Dispatch(initParticlesKernel, numberOfParticles / groupCount1D, 1, 1);

        shader.SetBuffer(updateParticlesKernel, "_ParticleBuffer", particleBuffer);
    }

	void InitializeTrail()
    {
		//Initialize compute buffer
		trailDensityBuffer = new ComputeBuffer[2];
		trailDensityBuffer[READ]  = new ComputeBuffer((int)(size.x * size.y * size.z), 4 * sizeof(float));
		trailDensityBuffer[WRITE] = new ComputeBuffer((int)(size.x * size.y * size.z), 4 * sizeof(float));

		shader.SetBuffer(initTrailKernel, "_TrailDensityWrite", trailDensityBuffer[WRITE]);

		shader.Dispatch(initTrailKernel, (int)size.x / groupCount3D, (int)size.y / groupCount3D, (int)size.z / groupCount3D);
	}

	void InitializeParticleMap() {

		//Position map
		if (_tmpParticlePositionMap != null)
			Destroy(_tmpParticlePositionMap);

		_tmpParticlePositionMap = new RenderTexture(particlePositionMap.width, particlePositionMap.height, 0, particlePositionMap.format);
		_tmpParticlePositionMap.enableRandomWrite = true;
		_tmpParticlePositionMap.Create();

		//Color map
		if (_tmpParticleColorMap != null)
			Destroy(_tmpParticleColorMap);

		_tmpParticleColorMap = new RenderTexture(particleColorMap.width, particleColorMap.height, 0, particleColorMap.format);
		_tmpParticleColorMap.enableRandomWrite = true;
		_tmpParticleColorMap.Create();

		//Velocity map
		if (_tmpParticleVelocityMap != null)
			Destroy(_tmpParticleVelocityMap);

		_tmpParticleVelocityMap = new RenderTexture(particleVelocityMap.width, particleVelocityMap.height, 0, particleVelocityMap.format);
		_tmpParticleVelocityMap.enableRandomWrite = true;
		_tmpParticleVelocityMap.Create();

		shader.SetVector("_ParticlePositionMapSize", new Vector2(_tmpParticlePositionMap.width, _tmpParticlePositionMap.height));
		shader.SetTexture(updateParticleMapKernel, "_ParticlePositionMap", _tmpParticlePositionMap);
		shader.SetTexture(updateParticleMapKernel, "_ParticleColorMap", _tmpParticleColorMap);
		shader.SetTexture(updateParticleMapKernel, "_ParticleVelocityMap", _tmpParticleVelocityMap);
		shader.SetBuffer(updateParticleMapKernel, "_ParticleBuffer", particleBuffer);

	}

	#endregion

	#region Runtime Functions

	void SwapBuffers(ComputeBuffer[] buffer) {
		ComputeBuffer tmp = buffer[READ];
		buffer[READ] = buffer[WRITE];
		buffer[WRITE] = tmp;
	}

	void UpdateRuntimeParameters()
    {
        sensorAngle = sensorAngleDegrees * Mathf.Deg2Rad;
        rotationAngle = rotationAngleDegrees * Mathf.Deg2Rad;
		shader.SetFloat("_AbsoluteTime", Time.time);
		if (useSensorRotationMaster) {
			shader.SetVector("_SensorAngle", Vector2.one * sensorRotationMaster);
			shader.SetVector("_RotationAngle", Vector2.one * sensorRotationMaster);
		} else {
			shader.SetVector("_SensorAngle", sensorAngle);
			shader.SetVector("_RotationAngle", rotationAngle);
		}
        shader.SetFloat("_SensorOffsetDistance", sensorOffsetDistance);
        shader.SetFloat("_StepSize", stepSize);
        shader.SetFloat("_TrailDecay", trailDecay);
		shader.SetBool("_DiffuseTrail", diffuseTrail);
		shader.SetFloat("_TrailDiffuseSpeed", trailDiffuseSpeed);
		shader.SetFloat("_TrailRepulsion", trailRepulsion);
		shader.SetFloat("_RandomRotationProbability", randomRotationProbability);
		shader.SetFloat("_TrailToSizeImportance", trailToSizeImportance);
		
	}

    void UpdateParticles()
    {
		SwapBuffers(trailDensityBuffer);

		shader.SetBuffer(updateParticlesKernel, "_TrailDensityRead", trailDensityBuffer[READ]);
		shader.SetBuffer(updateParticlesKernel, "_TrailDensityWrite", trailDensityBuffer[WRITE]);

		shader.Dispatch(updateParticlesKernel, numberOfParticles / groupCount1D, 1, 1);
	}

    void UpdateTrail()
    {
		SwapBuffers(trailDensityBuffer);

		shader.SetBuffer(updateTrailKernel, "_TrailDensityRead", trailDensityBuffer[READ]);
		shader.SetBuffer(updateTrailKernel, "_TrailDensityWrite", trailDensityBuffer[WRITE]);

		//SDF
		//shader.SetTexture(updateTrailKernel, "_MeshVoxels", meshToSDF.outputRenderTexture);
		//shader.SetInt("_MeshSDFResolution", meshToSDF.sdfResolution);
		//shader.SetFloat("_MeshImportance", meshImportance);

		shader.Dispatch(updateTrailKernel, (int)size.x / groupCount3D, (int)size.y / groupCount3D, (int)size.z / groupCount3D);
	}

	void UpdateParticleMap() {

		shader.Dispatch(updateParticleMapKernel, numberOfParticles / groupCount1D, 1, 1);

		Graphics.CopyTexture(_tmpParticlePositionMap, particlePositionMap);
		Graphics.CopyTexture(_tmpParticleColorMap, particleColorMap);
		Graphics.CopyTexture(_tmpParticleVelocityMap, particleVelocityMap);
	}

	#endregion
}
