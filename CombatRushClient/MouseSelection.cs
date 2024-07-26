using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StateMachine;

namespace CombatRushClient;

public class MouseSelection : IGameEntity
{
    private Texture2D _texture;
    private Point _originalPos;
    private Point _selectionPos;
    private Point _outerSelectionBoarder;
    private Fsm<MouseSelectionState, ButtonState> _stateMachine;

    private ISelectable[] _recentlySelectedArtifacts;
    public ISelectable[] Artifacts { get; set; }
    public MouseSelection(GraphicsDevice graphicsDevice)
    {
        _texture = new Texture2D(graphicsDevice, 1, 1);
        _texture.SetData(new[] { Color.LimeGreen });

        _recentlySelectedArtifacts = Array.Empty<ISelectable>();

        _stateMachine = Fsm<MouseSelectionState, ButtonState>.Builder(MouseSelectionState.NotSelecting)
            //
            .State(MouseSelectionState.NotSelecting).TransitionTo(MouseSelectionState.Selecting)
            .On(ButtonState.Pressed)
            //
            .State(MouseSelectionState.Selecting).OnEnter(_ =>
            {
                _originalPos = Mouse.GetState().Position;
            })
            .OnExit(_ =>
            {
                foreach (var recentlySelected in _recentlySelectedArtifacts)
                {
                    recentlySelected.OnDeselect();
                }
                var rect = new Rectangle(_selectionPos,_outerSelectionBoarder);
                var selected = Artifacts.Where(selectable => rect.Contains(selectable.Position + new Vector2(96,120))).ToArray();
                _recentlySelectedArtifacts = selected;
                foreach (var entity in selected)
                {
                    entity.OnSelect();
                }
            })
            .Update(_ =>
            {
                _selectionPos = _originalPos;
                _outerSelectionBoarder = Mouse.GetState().Position - _originalPos;
                if (_outerSelectionBoarder.X < 0)
                {
                    _outerSelectionBoarder.X = Math.Abs(_outerSelectionBoarder.X);
                    _selectionPos.X = _originalPos.X - _outerSelectionBoarder.X;
                }
                if (_outerSelectionBoarder.Y < 0)
                {
                    _outerSelectionBoarder.Y = Math.Abs(_outerSelectionBoarder.Y);
                    _selectionPos.Y = _originalPos.Y - _outerSelectionBoarder.Y;
                }
            })
            .TransitionTo(MouseSelectionState.NotSelecting).On(ButtonState.Released)
            .Build();
    }

    public void Update(GameTime gameTime)
    {
        _stateMachine.Trigger(Mouse.GetState().LeftButton);
        _stateMachine.Update(gameTime.ElapsedGameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_stateMachine.Current.Identifier == MouseSelectionState.Selecting)
            spriteBatch.Draw(_texture, new Rectangle(_selectionPos, _outerSelectionBoarder), Color.LimeGreen * 0.5f);
    }
}


public enum MouseSelectionState
{
    NotSelecting,
    Selecting
}