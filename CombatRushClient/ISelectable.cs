using Microsoft.Xna.Framework;

namespace CombatRushClient;

public interface ISelectable : IPositionable
{
    public void OnSelect();
    public void OnDeselect();
}