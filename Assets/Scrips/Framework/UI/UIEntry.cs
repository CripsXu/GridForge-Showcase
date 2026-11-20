using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIEntry
{
    public UIView View { get;}
    public string Address { get; }

    public UIEntry(UIView view, string address)
    {
        View = view;
        Address = address;
    }
}
