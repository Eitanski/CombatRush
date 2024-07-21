using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Aseprite;
using MonoGame.Extended;
using Stateless;
using StateMachine;

namespace CombatRushClient;

public class Worker : IEntity
{
    public Fsm<WorkerAnimations, ButtonState> StateMachine { get; set; }

    private Dictionary<WorkerAnimations, AnimatedSprite> _animations;

    private WorkerAnimations _currentAnimation;

    private const float MovementFactor = 2.5f;
    public Vector2 Velocity { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 LastDesignatedPosition { get; set; }
    public Vector2 LastOriginalPosition { get; set; }

    public Worker(SpriteSheet spriteSheet)
    {
        // StateMachine = new StateMachine<WorkerAnimations, ButtonState>(WorkerAnimations.Idle);
        StateMachine = Fsm<WorkerAnimations, ButtonState>.Builder((WorkerAnimations.Idle))
            //
            .State(WorkerAnimations.Idle).OnEnter(args =>
            {
                SetAnimation(args.To.Identifier);
                Velocity = Vector2.Zero;
            })
            .TransitionTo(WorkerAnimations.InitiateRun).On(ButtonState.Pressed)
            //
            .State(WorkerAnimations.InitiateRun).OnExit(_ => Velocity = Vector2.Zero).TransitionTo(WorkerAnimations.Run)
            .On(ButtonState.Released)
            //
            .State(WorkerAnimations.Run).OnEnter(args =>
            {
                SetAnimation(args.To.Identifier);
                var mousePos = Mouse.GetState().Position.ToVector2();
                mousePos -= new Vector2(96, 96);
                LastOriginalPosition = Position;
                LastDesignatedPosition = mousePos;
                Velocity = (mousePos - Position).NormalizedCopy() * MovementFactor;
            }).TransitionTo(WorkerAnimations.InitiateRun).On(ButtonState.Pressed)
            .Update(updateArgs =>
            {
                if ((Position - LastOriginalPosition).Length() >
                    (LastDesignatedPosition - LastOriginalPosition).Length())
                    updateArgs.Machine.JumpTo(WorkerAnimations.Idle);
            })
            .Build();
        //
        // StateMachine.OnTransitioned(transition =>
        // {
        //     if (transition.Destination != WorkerAnimations.InitiateRun)
        //         SetAnimation(transition.Destination);
        // });

        // StateMachine.Configure(WorkerAnimations.Idle).Ignore(ButtonState.Released)
        //     .Permit(ButtonState.Pressed, WorkerAnimations.InitiateRun);
        //
        // StateMachine.Configure(WorkerAnimations.InitiateRun).OnExit(_ => Velocity = Vector2.Zero)
        //     .Ignore(ButtonState.Pressed).Permit(ButtonState.Released, WorkerAnimations.Run);
        //
        // StateMachine.Configure(WorkerAnimations.Run).Ignore(ButtonState.Released)
        //     .OnEntry(() =>
        //     {
        //         var mousePos = Mouse.GetState().Position.ToVector2();
        //         mousePos -= new Vector2(96, 96);
        //         LastOriginalPosition = Position;
        //         LastDesignatedPosition = mousePos;
        //         Velocity = (mousePos - Position).NormalizedCopy() * MovementFactor;
        //     })
        //     .Permit(ButtonState.Pressed, WorkerAnimations.InitiateRun);

        _animations = new Dictionary<WorkerAnimations, AnimatedSprite>
        {
            { WorkerAnimations.Idle, spriteSheet.CreateAnimatedSprite("Idle") },
            { WorkerAnimations.Run, spriteSheet.CreateAnimatedSprite("Run") },
            { WorkerAnimations.CarryRun, spriteSheet.CreateAnimatedSprite("Run_Carry") },
            { WorkerAnimations.CarryIdle, spriteSheet.CreateAnimatedSprite("Idle_Carry") },
            { WorkerAnimations.Build, spriteSheet.CreateAnimatedSprite("Build") },
            { WorkerAnimations.Cut, spriteSheet.CreateAnimatedSprite("Cut") }
        };

        SetAnimation(WorkerAnimations.Idle);
    }

    private void SetAnimation(WorkerAnimations animation)
    {
        _currentAnimation = animation;
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

public enum WorkerStates
{
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

public enum WorkerAnimations
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