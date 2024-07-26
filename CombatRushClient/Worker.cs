using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Aseprite;
using MonoGame.Extended;
using StateMachine;

namespace CombatRushClient;

public class Worker : IUnit
{
    public Fsm<WorkerState, ButtonState> StateMachine { get; set; }

    private Dictionary<WorkerAnimation, AnimatedSprite> _animations;

    private WorkerAnimation _currentAnimation;

    private const float MovementFactor = 2.5f;
    public Vector2 Velocity { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 LastDesignatedPosition { get; set; }
    public Vector2 LastOriginalPosition { get; set; }

    private bool _isSelected;

    public Worker(SpriteSheet spriteSheet)
    {
        StateMachine = Fsm<WorkerState, ButtonState>.Builder(WorkerState.Idle)
            .State(WorkerState.Idle).OnEnter(_ =>
            {
                SetAnimation(WorkerAnimation.Idle);
                Velocity = Vector2.Zero;
            })
            .TransitionTo(WorkerState.InitiateRunning).On(ButtonState.Pressed).If(_ => _isSelected)
            //
            .State(WorkerState.InitiateRunning).OnExit(_ => Velocity = Vector2.Zero).TransitionTo(WorkerState.Running)
            .On(ButtonState.Released).If(_ => _isSelected)
            //
            .State(WorkerState.Running).OnEnter(_ =>
            {
                var mousePos = Mouse.GetState().Position.ToVector2();
                mousePos -= new Vector2(96, 120);
                LastOriginalPosition = Position;
                LastDesignatedPosition = mousePos;
                Velocity = (mousePos - Position).NormalizedCopy() * MovementFactor;
                SetAnimation(WorkerAnimation.Run);
            }).TransitionTo(WorkerState.InitiateRunning).On(ButtonState.Pressed).If(_ => _isSelected)
            .Update(updateArgs =>
            {
                if ((Position - LastOriginalPosition).Length() >
                    (LastDesignatedPosition - LastOriginalPosition).Length())
                    updateArgs.Machine.JumpTo(WorkerState.Idle);
            })
            .Build();

        _animations = new Dictionary<WorkerAnimation, AnimatedSprite>
        {
            { WorkerAnimation.Idle, spriteSheet.CreateAnimatedSprite("Idle") },
            { WorkerAnimation.Run, spriteSheet.CreateAnimatedSprite("Run") },
            { WorkerAnimation.CarryRun, spriteSheet.CreateAnimatedSprite("Run_Carry") },
            { WorkerAnimation.CarryIdle, spriteSheet.CreateAnimatedSprite("Idle_Carry") },
            { WorkerAnimation.Build, spriteSheet.CreateAnimatedSprite("Build") },
            { WorkerAnimation.Cut, spriteSheet.CreateAnimatedSprite("Cut") }
        };

        SetAnimation(WorkerAnimation.Idle);
    }

    public void OnSelect()
    {
        _isSelected = true;
        SetSelectionOverlay();
    }

    public void OnDeselect()
    {
        _isSelected = false;
        RemoveSelectionOverlay();
    }

    private void SetSelectionOverlay()
    {
    }

    private void RemoveSelectionOverlay()
    {
    }

    private void SetAnimation(WorkerAnimation animation)
    {
        _currentAnimation = animation;
        _animations[_currentAnimation].FlipHorizontally = Velocity.X <= 0;
        _animations[_currentAnimation].Play();
    }

    public void Update(GameTime gameTime)
    {
        StateMachine.Trigger(Mouse.GetState().RightButton);
        StateMachine.Update(gameTime.ElapsedGameTime);

        _animations[_currentAnimation].Update(gameTime);
        Position += Velocity;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_animations[_currentAnimation], Position);
    }
}

public enum WorkerState
{
    NotSelected,
    Selected,
    Building,
    Cutting,
    Attacking,
    Running,
    InitiateRunning,
    Idle,
    Carry,
    CarryRun,
    CarryIdle
}

public enum WorkerAnimation
{
    Build,
    Cut,
    Attack,
    InitiateRun,
    Run,
    Idle,
    CarryRun,
    CarryIdle
}