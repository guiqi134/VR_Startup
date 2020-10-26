﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallBounce : MonoBehaviour
{
    private AudioSource bounceSound;

    
    // Start is called before the first frame update
    void Start()
    {
        bounceSound = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision col)
    {
        bounceSound.volume = Mathf.Clamp01(col.relativeVelocity.magnitude / 10.0f);
        //audio_source.volume = audio_source.volume * audio_source.volume;
        bounceSound.Play();      
    }
}