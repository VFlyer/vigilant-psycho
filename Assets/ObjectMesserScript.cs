using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectMesserScript : MonoBehaviour {
	public bool randomlyRotateText, randomlyResizeText, randomlyRotateObjects, mixTextures, offsetTextures, rescaleTextures;
	public Vector3 randomOffset;
	public float intensity;
	public Transform[] affectedTransforms;
	public TextMesh[] affectedTextMeshes;
	public MeshRenderer[] affectedRenderers;
	public Texture[] alternativeTextures;

	private Quaternion[] storedInitialRotationsObjects, storedInitialRotationsTexts;
	private float[] storedInitialCharSizes;
	private Texture[] storedInitialTextures;
	private Vector2[] storedInitialOffsets, storedInitialScales;

	IEnumerator intensityHandler;

	void Awake () {
		storedInitialRotationsObjects = affectedTransforms.Select(a => a.localRotation).ToArray();
		storedInitialCharSizes = affectedTextMeshes.Select(a => a.characterSize).ToArray();
		storedInitialRotationsTexts = affectedTextMeshes.Select(a => a.transform.localRotation).ToArray();
		storedInitialTextures = affectedRenderers.Select(a => a.material.mainTexture).ToArray();
		storedInitialOffsets = affectedRenderers.Select(a => a.material.mainTextureOffset).ToArray();
		storedInitialScales = affectedRenderers.Select(a => a.material.mainTextureScale).ToArray();
	}
	public void RevertAllAffectedObjects()
    {
        for (var x = 0; x < affectedTransforms.Length; x++)
			affectedTransforms[x].localRotation = storedInitialRotationsObjects[x];
		for (var x = 0; x < affectedTextMeshes.Length; x++)
		{
			affectedTextMeshes[x].characterSize = storedInitialCharSizes[x];
			affectedTextMeshes[x].transform.localRotation = storedInitialRotationsTexts[x];
		}
        for (var x = 0; x < affectedTransforms.Length; x++)
			affectedTransforms[x].localRotation = storedInitialRotationsObjects[x];
		for (var x = 0; x < affectedRenderers.Length; x++)
		{
			affectedRenderers[x].material.mainTexture = storedInitialTextures[x];
			affectedRenderers[x].material.mainTextureOffset = storedInitialOffsets[x];
			affectedRenderers[x].material.mainTextureScale = storedInitialScales[x];
		}
	}

	public void StepIntensity()
    {
		for (var x = 0; randomlyRotateObjects && x < affectedTransforms.Length; x++)
		{
			if (Random.value < intensity / 5)
			affectedTransforms[x].localRotation *= Quaternion.Euler(randomOffset * intensity * Random.value);
		}
		for (var x = 0; randomlyRotateText && x < affectedTextMeshes.Length; x++)
		{
			if (Random.value < intensity / 5)
				affectedTextMeshes[x].transform.localRotation *= Quaternion.Euler(randomOffset * intensity * Random.value);
		}
		for (var x = 0; randomlyResizeText && x < affectedTextMeshes.Length; x++)
		{
			if (Random.value < intensity / 5)
				affectedTextMeshes[x].characterSize = storedInitialCharSizes[x] * (6f - Random.value * intensity) / 5f;
		}
		for (var x = 0; mixTextures && x < affectedRenderers.Length; x++)
		{
			if (Random.value < intensity / 5)
				affectedRenderers[x].material.mainTexture = alternativeTextures.PickRandom();
		}
		for (var x = 0; offsetTextures && x < affectedRenderers.Length; x++)
		{
			if (Random.value < intensity / 5)
				affectedRenderers[x].material.mainTextureOffset = Random.insideUnitCircle;
		}
		for (var x = 0; rescaleTextures && x < affectedRenderers.Length; x++)
		{
			if (Random.value < intensity / 5)
				affectedRenderers[x].material.mainTextureScale = Random.insideUnitCircle;
		}
	}

	public void Intensify(float delay)
    {
		if (intensityHandler != null)
			StopCoroutine(intensityHandler);
		intensityHandler = MessItUp(delay);
		StartCoroutine(intensityHandler);
    }
	public void StopIntensify()
    {
		if (intensityHandler != null)
			StopCoroutine(intensityHandler);
    }

	IEnumerator MessItUp(float stepDelay)
    {
		while (true)
		{
			StepIntensity();
			yield return new WaitForSeconds(stepDelay);
		}
    }
}
