#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

#include "UnityCG.cginc"

// Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
uint Hash(uint s) {
	s ^= 2747636419u;
	s *= 2654435769u;
	s ^= s >> 16;
	s *= 2654435769u;
	s ^= s >> 16;
	s *= 2654435769u;
	return s;
}

float Random(uint seed) {
	return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

float NRand(float2 co){
	return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
}

float NRand(float2 uv, float salt) {
	uv += float2(salt, 0.0);
	return NRand(uv);
}

float3 NRand3(float2 seed){
	float t = sin(seed.x + seed.y * 1e3);
	return float3(frac(t*1e4), frac(t*1e6), frac(t*1e5));
}

float3 RandomOrth(float2 seed) {
	// float u = (nrand(seed) + 1.0) * 0.5;
	float u = NRand(seed);

	float3 axis;

	if (u < 0.166) axis = float3(0, 0, 1);
	else if (u < 0.332) axis = float3(0, 0, -1);
	else if (u < 0.498) axis = float3(0, 1, 0);
	else if (u < 0.664) axis = float3(0, -1, 0);
	else if (u < 0.83) axis = float3(-1, 0, 0);
	else axis = float3(1, 0, 0);

	return axis;
}

float3 RandomPositiveOrth(float2 seed) {
	float u = (NRand(seed) + 1) * 0.5;

	float3 axis;

	if (u < 0.333) axis = float3(0, 0, 1);
	else if (u < 0.666) axis = float3(0, 1, 0);
	else axis = float3(1, 0, 0);

	return axis;
}

// Uniformaly distributed points on a unit sphere
// http://mathworld.wolfram.com/SpherePointPicking.html
float3 RandomPointOnSphere(float2 uv) {
	float u = NRand(uv) * 2 - 1;
	float theta = NRand(uv + 0.333) * UNITY_PI * 2;
	float u2 = sqrt(1 - u * u);
	return float3(u2 * cos(theta), u2 * sin(theta), u);
}

//Uniformly distributed points inside a unit sphere
//https://math.stackexchange.com/questions/87230/picking-random-points-in-the-volume-of-sphere-with-uniform-probability
float3 RandomPointInSphere(uint seed) {

	float x1 = Random(seed * 786) * 2.0f - 1.0f;
	float x2 = Random(seed * 456) * 2.0f - 1.0f;
	float x3 = Random(seed * 123) * 2.0f - 1.0f;

	float U = Random(seed * 863);
	U = pow(U, 1.0f / 3.0f);

	float3 output = float3(x1, x2, x3);

	return normalize(output) * U;

}

float RandomSign(float v)
{
	float x = Random(v);
	return (step(0.5, x) * 2.0) - 1.0;
}

#endif // RANDOM_INCLUDED
