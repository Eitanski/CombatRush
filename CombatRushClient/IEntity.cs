using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StateMachine;

namespace CombatRushClient;

public interface IEntity
{
    public void Update(GameTime gameTime);
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}