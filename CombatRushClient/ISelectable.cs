using Microsoft.Xna.Framework;

namespace CombatRushClient;

public interface ISelectable
{
    public void OnSelect();
    public void OnDeselect();
    public Vector2 Position { get; set; }
}