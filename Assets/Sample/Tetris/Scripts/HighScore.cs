using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighScore : MonoBehaviour {

    public static int highScore = 0;

    public static void Set(int score) {
        if (score > highScore) {
            highScore = score;
        }
        
    }

    public static string Get() { 
        return $"{highScore:D8}";
    }

}
