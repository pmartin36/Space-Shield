﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PointBeam : MonoBehaviour {

	public Colors GameColor;
	private Color color;
	private float totalLifetime;
	private float currentLifetime;

	public float BeamDistance;
	public float BeamWidth;

	MeshFilter beamMeshFilter;
	MeshRenderer beamMeshRenderer;

	SpriteRenderer spriteRenderer;

	public float PulseOffset;

	ParticleSystem plusSignParticleSystem;
	ParticleSystem sparksParticleSystem;

	//private AudioSource audio;
	float nextPointDing = 0;

	bool Despawning = false;
	bool BeamDespawned = false;

	// Use this for initialization
	void Start () {
		

		//audio = GetComponent<AudioSource>();
	}

	public void Init(PointBeamParameters p) {
		transform.position = new Vector2(p.x, p.y);
		Init(p.GameColor, p.Duration);	
		
		(GameManager.Instance.ContextManager as LevelManager).TotalAvailablePoints += (int)(400 * p.Duration);
	}

	public void Init(Colors c, float lifetime) {
		GameColor = c;
		totalLifetime = lifetime;
		currentLifetime = 0;

		color = Utils.GetColorFromGameColor(GameColor);

		beamMeshFilter = GetComponentInChildren<MeshFilter>();
		beamMeshRenderer = GetComponentInChildren<MeshRenderer>();
		beamMeshRenderer.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Sprites/beam"));

		spriteRenderer = GetComponent<SpriteRenderer>();

		var pss = GetComponentsInChildren<ParticleSystem>();
		plusSignParticleSystem = pss.First(p => p.shape.shapeType == ParticleSystemShapeType.Mesh);
		sparksParticleSystem = pss.First(p => p.shape.shapeType == ParticleSystemShapeType.Hemisphere);

		var main = plusSignParticleSystem.main;
		main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);

		Color centerColor = Color.white;
		Color exteriorColor = color;
		if (GameColor == Colors.Blue) {
			centerColor = Color.cyan;
		}
		else if (GameColor == Colors.Green) {
			exteriorColor = color / 2f;
			exteriorColor.a = 1f;
		}

		beamMeshRenderer.material.SetColor("_ExteriorColor", exteriorColor);
		beamMeshRenderer.material.SetColor("_CenterColor", centerColor);
		spriteRenderer.material.SetColor("_ExteriorColor", exteriorColor);
		spriteRenderer.material.SetColor("_CenterColor", centerColor);
	}

	private void SetSparkParticleProperties(Shield s, Vector2 t) {
		var main = sparksParticleSystem.main;

		if (s == null) {
			main.startColor = new ParticleSystem.MinMaxGradient(this.color);
		}
		else {
			main.startColor = new ParticleSystem.MinMaxGradient(Utils.GetColorFromGameColor(s.GameColor), this.color);
		}

		sparksParticleSystem.transform.position = t;
	}

	// Update is called once per frame
	void Update () {
		//beamMeshRenderer.material.SetFloat("_Offset", spriteRenderer.material.GetFloat("_Offset"));

		float beamHalfWidth = BeamWidth / 2f;
		float zangle = Utils.VectorToAngle(transform.position);
		transform.localRotation = Quaternion.Euler(0,0, zangle);

		float disttocenter = Vector2.Distance(transform.position, Vector2.zero);
		float dist = BeamDistance;

		RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, 
													new Vector2( BeamWidth, BeamWidth),
													-zangle, 
													-transform.position,
													disttocenter, 			
													1 << LayerMask.NameToLayer("Shield")).
														Where(h => Vector2.Angle(h.normal, transform.position) < 20)
														.OrderBy(h => h.distance).ToArray();

		bool gettingPoints = false;
		for(int i = 0; i < hits.Length; i++) {
			RaycastHit2D hit = hits[i];
			Shield s = hit.collider.GetComponent<Shield>();
			if(s != null) {
				//display laser beam splash at location on shield
				//update distance
				dist = Mathf.Min(dist, Vector2.Distance(transform.position, hit.point) * 1 / transform.localScale.x);
				SetSparkParticleProperties(s, hit.point);

				if (s.GameColor == GameColor) {
					gettingPoints = true;
					break;
				}
				continue;						
			}
			else if( hits.Length == 1 ) {
				//if we have one hit and it's a core
				if(hit.collider.GetComponent<Core>() != null) {
					dist = Mathf.Min(dist, Vector2.Distance(transform.position, hit.point) * 1 / transform.localScale.x);
					SetSparkParticleProperties(null, hit.point);
				}
			}
		}

		var em = plusSignParticleSystem.emission;
		if(gettingPoints && !Despawning) {
			em.enabled = true;
			(GameManager.Instance.ContextManager as LevelManager).PointManager.IncrementPoints((int)(400 * Time.deltaTime), "Point Beam", color);
		}
		else {
			em.enabled = false;
		}

		if(!BeamDespawned) {
			Mesh m = new Mesh();
			m.name = "Beam";
			m.vertices = new Vector3[] {
				new Vector3(0, -beamHalfWidth, 10f),
				new Vector3(0, beamHalfWidth, 10f),
				new Vector3(-dist, -beamHalfWidth, 10f),
				new Vector3(-dist, beamHalfWidth, 10f),
			};
			m.uv = new Vector2[] {
				new Vector2(1,0),
				new Vector2(1,1),
				new Vector2(0,0),
				new Vector2(0,1),
			};
			m.triangles = new int[] {
				0, 2, 1,
				1, 2, 3
			};
			m.RecalculateNormals();
			beamMeshFilter.mesh = m;
		
			var shape = plusSignParticleSystem.shape;	
			Mesh m2 = new Mesh();
			m2.vertices = m.vertices.Select( b => new Vector3(b.x*1.1f,b.y*2,b.z) ).ToArray();
			m2.triangles = m.triangles;
			m.RecalculateNormals();
			shape.mesh = m2;
		}

		currentLifetime += Time.deltaTime;
		if(currentLifetime >= totalLifetime && !Despawning) {
			StartCoroutine(Despawn());
		}
	}

	IEnumerator Despawn() {
		float startTime = Time.time;
		float jTime = 1f;

		float startBeamWidth = BeamWidth;
		float startBeamDistance = BeamDistance;

		var sparkem = sparksParticleSystem.emission;
		sparkem.enabled = false;

		var plusem = plusSignParticleSystem.emission;
		plusem.enabled = false;

		Despawning = true;
		GetComponent<Animator>().StopPlayback();

		while (Time.time - startTime < jTime + Time.deltaTime) {
			float ttime = (Time.time - startTime) / jTime;
			BeamWidth = Mathf.Lerp(startBeamWidth, 0, ttime);
			BeamDistance = Mathf.Lerp(startBeamDistance, 0, ttime);
			yield return new WaitForEndOfFrame();
		}

		BeamDespawned = true;
		startTime = Time.time;
		Color endColor = new Color(1,1,1,0);
		while(Time.time - startTime < jTime + Time.deltaTime) {
			float ttime = (Time.time - startTime) / jTime;
			spriteRenderer.color = Color.Lerp(Color.white, Color.clear, ttime);
			yield return new WaitForEndOfFrame();
		}

		Destroy(this.gameObject);
	}
}
