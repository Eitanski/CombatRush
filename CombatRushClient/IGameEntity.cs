using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CombatRushClient;

public interface IGameEntity
{
    public void Update(GameTime gameTime);
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}

public enum InputState
{
    InSelectionRange,
    RightClick,
    RightRelease,
    LeftClick,
    LeftRelease
}