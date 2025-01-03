using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.UI;

public class LeaderBoard : MonoBehaviour
{
    private Transform Container,Template, YourPos;

    private void Awake()
    {
        Container = transform.Find("Container");
        Template = Container.Find("ScoreEntry");
        YourPos = Container.Find("YourScoreEntry");
        Template.gameObject.SetActive(false);
        float templateHigh = 110f;

        for(int i = 0; i < 8; i++) 
        {
           Transform entryTransform = Instantiate(Template, Container);
           RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
            entryRectTransform.anchoredPosition = new Vector2(0,-templateHigh * i);
            entryTransform.gameObject.SetActive(true);

            /*int rank = i + 1;
            string rankString;
            switch (rank)
            {
                default: rankString = rank + "TH"; break;
                case 1: rankString = "1ST"; break;
                case 2: rankString = "2ND"; break;
                case 3: rankString = "3RD"; break;

            }
            entryTransform.Find("Position").GetComponent<TextMeshProUGUI>().text = rankString;*/
            int score = Random.Range(0, 5000);
            entryTransform.Find("Score").GetComponent<TextMeshProUGUI>().text = score.ToString();

            string name = "AAA";
            entryTransform.Find("Name").GetComponent<TextMeshProUGUI>().text = name;

        }
        int scoreYours = Random.Range(0, 5000);
        YourPos.Find("Score").GetComponent<TextMeshProUGUI>().text = scoreYours.ToString();

        string nameYours = "You";
        YourPos.Find("Name").GetComponent<TextMeshProUGUI>().text = nameYours;

        int rankYours = Random.Range(0, 100);
        YourPos.Find("Rank").GetComponent<TextMeshProUGUI>().text = rankYours.ToString();

    }
}
