﻿using UnityEngine;

public class CreateGame : MonoBehaviour
{
    public GameObject game;
    public Canvas menu;

    private void Awake()
    {
        game.SetActive(false);
    }
}