using AsepriteDotNet;
using Microsoft.Xna.Framework;
using MonoGame.Aseprite.Utils;

namespace CombatRushClient;

public class SelectionOverlay : IPositionable
{
    public Sprite TopRight { get; set; }
    public Sprite TopLeft { get; set; }
    public Sprite BottonRight { get; set; }
    public Sprite BottomLeft { get; set; }
    public Vector2 Position { get; set; }

    // public SelectionOverlay Create()
    // {
    //     
    // }

}