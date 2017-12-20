﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	private Vector3 shakeDirection;
	public float shakeMagnitude;
	private float shakeDampening = 25f;

	Animator anim;

	public Material PostMaterial;

	// Use this for initialization
	void Start () {
		GameManager.Instance.MainCameraController = this;
		anim = GetComponent<Animator>();
		shakeDampening = 25f;
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 shake = shakeDirection * shakeMagnitude / shakeDampening;
		transform.position = new Vector3(shake.x, shake.y, -10);
	}

	public void Shake(Vector3 direction) {
		shakeDirection = direction;
		anim.Play("screenshake");
	}

	public void Tremble() {
		anim.Play("screentremble");
	}

	public void OnRenderImage(RenderTexture src, RenderTexture dest) {
		if(PostMaterial != null) {
			Graphics.Blit(src, dest, PostMaterial);
		}
		else {
			Graphics.Blit(src, dest);
		}
	}
	/*
	IEnumerator ScreenShake(Vector3 d) {
		float shakeTime = 0.1f;
		float startTime = Time.time;

		Vector3 startPosition = transform.position;

		d /= 35f;

		while( Time.time - startTime < (shakeTime / 3f) + Time.deltaTime) {
			float jTime = (Time.time - startTime) * 3f / shakeTime;
			transform.position = Vector3.Lerp( startPosition, startPosition + d, jTime); 
			yield return new WaitForEndOfFrame();
		}

		startTime = Time.time;
		while (Time.time - startTime < (shakeTime / 3f) + Time.deltaTime) {
			float jTime = (Time.time - startTime) * 3f / shakeTime;
			transform.position = Vector3.Lerp(startPosition + d, startPosition - d/2f, jTime);
			yield return new WaitForEndOfFrame();
		}

		startTime = Time.time;
		while (Time.time - startTime < (shakeTime / 3f) + Time.deltaTime) {
			float jTime = (Time.time - startTime) * 3f / shakeTime;
			transform.position = Vector3.Lerp(startPosition - d / 2f, startPosition, jTime);
			yield return new WaitForEndOfFrame();
		}
	}
	*/
}
