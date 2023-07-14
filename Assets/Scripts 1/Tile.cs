using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour {

    public KMSelectable selectable;
    public KMHighlightable highlight;
    public MeshRenderer xMesh, oMesh;
    public TextMesh cbText;
    public int position;

    private ShapeColor _color;
    private TileValue _shape;
    private bool _interactable = true;
    private static Dictionary<ShapeColor, Color32> materialColors = new Dictionary<ShapeColor, Color32>()
    {
        { ShapeColor.Gray, new Color32(0x6E, 0x6E, 0x6E, 0xFF) },
        { ShapeColor.Red, new Color32(0xD8, 0x4F, 0x4F, 0xFF) },
        { ShapeColor.Blue, new Color32(0x4F, 0x86, 0xD8, 0xFF) },
        { ShapeColor.Yellow, new Color32(0xD8, 0xC8, 0x4F, 0xFF) }
    };
    public ShapeColor Color
    {
        get { return _color; }
        set
        {
            _color = value;
            xMesh.material.color = materialColors[_color];
            oMesh.material.color = materialColors[_color];
            cbText.text = _color == ShapeColor.Gray ? "" : _color.ToString()[0].ToString();
        }
    }
    public TileValue Shape
    {
        get { return _shape; }
        set
        {
            _shape = value;
            xMesh.enabled = _shape == TileValue.X;
            oMesh.enabled = _shape == TileValue.O;
            IsInteractable = _shape == TileValue.None;
        }
    }
    public bool IsInteractable
    {
        get { return _interactable; }
        set
        {
            _interactable = value;
            highlight.transform.localPosition = (_interactable ? 10 : -1000) * Vector3.forward;
        }
    }
    public void ClearTile()
    {
        xMesh.enabled = false;
        oMesh.enabled = false;
        cbText.text = "";
    }
    public void SetTile(TileValue shape, ShapeColor color)
    {
        Shape = shape;
        Color = color;
    }
    public void SetColorblind(bool cbOn)
    {
        cbText.gameObject.SetActive(cbOn);
    }

}
