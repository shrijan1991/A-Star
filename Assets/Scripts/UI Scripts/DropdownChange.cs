using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownChange : MonoBehaviour {
    
    public void onChange() {
        Dropdown cur = GameObject.Find("AddToMap").GetComponent<Dropdown>();
        GameObject unitSize = GameObject.Find("unitSize");
        if (cur.value == 2)
        {
            unitSize.SetActive(true);
        }
        else {
            unitSize.SetActive(false);
        }
    }
}
