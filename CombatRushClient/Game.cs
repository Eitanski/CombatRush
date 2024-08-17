using System.Linq;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Aseprite;

namespace CombatRushClient;

public class Game : Microsoft.Xna.Framework.Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private IGameEntity[] _entities;
    public static SpriteFont _font;

    public Game()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        AsepriteFile aseFile;
        using (var stream = TitleContainer.OpenStream(@"SpriteSheets\Pawn_Blue.aseprite"))
        {
            aseFile = AsepriteFileLoader.FromStream("Pawn_Blue", stream, preMultiplyAlpha: true);
        }

        _font = Content.Load<SpriteFont>("File");   

        var spriteSheet = aseFile.CreateSpriteSheet(GraphicsDevice);

        var selectableEntities = new IUnit[] { new Worker(spriteSheet) };
        var baseEntities = new IGameEntity[] { new MouseSelection(GraphicsDevice) { Artifacts = selectableEntities } };

        _entities = baseEntities.Concat(selectableEntities).ToArray();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Mouse.SetCursor(MouseCursor.FromTexture2D(Content.Load<Texture2D>("Cursor"), 22, 17));
    }

    protected override void Update(GameTime gameTime)
    {
        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();

        foreach (var entity in _entities)
        {
            entity.Draw(gameTime, _spriteBatch);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}