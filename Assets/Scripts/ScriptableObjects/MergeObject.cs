using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "MergeObject")]
public class MergeObject : ScriptableObject
{
    [SerializeField]
    private int scorePoint;
    public int  ScorePoint => scorePoint;

    [SerializeField]
    private int _rank;
    public int Rank => _rank;

    [SerializeField]
    private GameObject _nextObject;
    public GameObject NextObject => _nextObject;

    

}
