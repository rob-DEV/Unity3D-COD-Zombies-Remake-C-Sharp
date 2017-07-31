using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Perk : MonoBehaviour {

    public int price;


    void OnTriggerStay(Collider col)
    {
        if (col.tag == "Player")
        {
            UI ui = col.GetComponent<UI>();
            ui.PromptText.text = "Press F to buy Perk - $" + price;

            

            if (Input.GetButton("Use"))
            {
                Player player = col.GetComponent<Player>();
                if (player.GetScore() >= price && !player.Perks.hasJugg)
                {
                    //buy perk and update player
                    player.MinusScore(price);
                    player.Perks.hasJugg = true;
                }
            }
        }
    }
    void OnTriggerExit(Collider col)
    {
        if (col.tag == "Player")
        {
            col.GetComponent<UI>().PromptText.text = "";
        }
    }
}
