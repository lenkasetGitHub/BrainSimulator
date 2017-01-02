﻿using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using World.Atlas;
using World.GameActors;
using World.GameActors.GameObjects;
using World.GameActors.Tiles;

namespace World.Lua
{
    public class AvatarCommander
    {
        private readonly IAtlas m_atlas;
        private readonly IAvatar m_currentAvatar;
        private readonly LuaExecutor m_le;

        public AvatarCommander(LuaExecutor ex, IAtlas atlas)
        {
            m_atlas = atlas;

            ex.State.RegisterFunction("Vector", typeof(AvatarCommander).GetMethod("Vector"));

            m_currentAvatar = atlas.Avatars.Values.First();
            ex.State["avatar"] = m_currentAvatar;

            m_le = ex;
        }

        public void Goto(Vector2 position)
        {
            // ac:Goto(Vector(20,30))
            Func<object[], bool> f = GotoI;
            m_le.Do(f, position, 0.1f);
        }

        public void Goto(int x, int y)
        {
            // ac:Goto(10,10)
            Func<object[], bool> f = GotoI;
            m_le.Do(f, new Vector2(x + 0.5f, y + 0.5f), 0.1f);
        }

        public void Goto(Vector2 position, float distance)
        {
            if (distance <= 0) throw new ArgumentException("Distance must be positive.");
            // ac:Goto("Pinecone", 2)
            Func<object[], bool> f = GotoI;
            m_le.Do(f, position, distance);
        }

        public void Goto(string type, float distance = (float)0.1)
        {
            // ac:Goto("Pinecone", 1.2)
            GameActorPosition gameActorPosition = GetNearest(m_currentAvatar.Position, type);
            if (gameActorPosition == null) return;
            Vector2 position = gameActorPosition.Position;
            if (gameActorPosition.Actor is Tile)
            {
                position = Tile.Center((Vector2I)position);
            }
            Goto(position, distance);
        }

        public void GotoR(int x, int y)
        {
            // ac:Goto(10,10)
            Vector2 cuPos = m_currentAvatar.Position;
            Func<object[], bool> f = GotoI;
            Vector2 relative = new Vector2(x, y);
            m_le.Do(f, cuPos + relative, 0.01f);
        }

        public void GotoR(int x, int y, float distance)
        {
            if (distance <= 0) throw new ArgumentException("Distance must be positive.");
            // ac:Goto(10,10,0.5)
            Vector2 cuPos = m_currentAvatar.Position;
            Func<object[], bool> f = GotoI;
            Vector2 relative = new Vector2(x, y);
            m_le.Do(f, cuPos + relative, distance);
        }

        public void SetHeading(string heading)
        {
            string lower = heading.ToLower();
            string trim = lower.Trim();
            float r;
            switch (trim[0])
            {
                case 'n':
                    r = 0f;
                    break;
                case 'w':
                    r = MathHelper.Pi / 2;
                    break;
                case 's':
                    r = MathHelper.Pi;
                    break;
                case 'e':
                    r = - MathHelper.Pi / 2;
                    break;
                default:
                    throw new ArgumentException("Direction was not recognized.");
                    
            }
            RotateTo(r);
        }

        public void SetHeading(int heading)
        {
            float r = MathHelper.ToRadians(-heading);
            RotateTo(r);
        }

        public void Teleport(float x, float y)
        {
            m_currentAvatar.Position = new Vector2(x, y);
        }

        public void Teleport(Vector2 vector)
        {
            m_currentAvatar.Position = vector;
        }

        public void TeleportR(float x, float y)
        {
            m_currentAvatar.Position += new Vector2(x, y);
        }

        public void RotateTo(float finalAngle, float precision)
        {
            if (precision <= 0) throw new ArgumentException("Precision must be positive.");
            // ac:RotateRight(100)
            Func<object[], bool> f = RotateToI;
            m_le.Do(f, finalAngle, precision);
        }

        public void RotateTo(float finalAngle)
        {
            // ac:RotateRight(100)
            Func<object[], bool> f = RotateToI;
            m_le.Do(f, finalAngle, MathHelper.Pi / 160);
        }

        private bool GotoI(params object[] parameters)
        {
            Vector2 targetPosition = (Vector2)parameters[0];
            float targetDistance = (float)parameters[1];
            float distance = Vector2.Distance(targetPosition, m_currentAvatar.Position);
            if (targetDistance >= distance) return true;
            float targetAngle = -Vector2.AngleTo(m_currentAvatar.Position, targetPosition);
            RotateToI(targetAngle, MathHelper.Pi / 16);
            float wrappedAngle = MathHelper.WrapAngle(targetAngle - m_currentAvatar.Rotation);
            const float maxAngle = MathHelper.Pi / 5;
            if (-maxAngle < wrappedAngle && wrappedAngle < maxAngle)
            {
                m_currentAvatar.Direction = m_currentAvatar.Rotation;
                if (distance > 1)
                {
                    m_currentAvatar.DesiredSpeed = 1;
                }
                else
                {
                    m_currentAvatar.DesiredSpeed = distance * 0.3f;
                }
            }
            return false;
        }

        private bool RotateToI(params object[] a)
        {
            float targetRotation = (float) a[0];
            float precision = (float) a[1];
            float diff = CalculateDifferenceBetweenAngles(m_currentAvatar.Rotation, targetRotation);
            float absDiff = Math.Abs(diff);
            if (absDiff < precision) return true;
            if (MathHelper.WrapAngle(diff) < 0)
            {
                m_currentAvatar.DesiredLeftRotation = Math.Max((float)-Math.Sqrt(absDiff), -1);
            }
            else
            {
                m_currentAvatar.DesiredLeftRotation = Math.Min((float)Math.Sqrt(absDiff), 1);
            }
        
            return false;
        }

        public GameActorPosition GetNearest(Vector2 position, string type)
        {
            Type t = GameActor.GetType(type);

            for (int i = 1; i < 20; i++)
            {
                IEnumerable<Vector2I> chebyshevNeighborhood = Neighborhoods.VonNeumannNeighborhood((Vector2I)position, i);

                foreach (var x in chebyshevNeighborhood)
                {
                    foreach (GameActorPosition gameActorPosition in m_atlas.ActorsAt((Vector2)x))
                    {
                        if (gameActorPosition.Actor.GetType() == t)
                        {
                            return gameActorPosition;
                        }
                    }
                }
            }
            return null;
        }

        public GameActorPosition GetNearest(string type)
        {
            return GetNearest(m_currentAvatar.Position, type);
        }

        public static Vector2 Vector(float x, float y)
        {
            return new Vector2(x, y);
        }

        private float CalculateDifferenceBetweenAngles(float firstAngle, float secondAngle)
        {
            float difference = secondAngle - firstAngle;
            while (difference < -MathHelper.Pi) difference += MathHelper.Pi*2;
            while (difference > MathHelper.Pi) difference -= MathHelper.Pi*2;
            return difference;
        }
    }
}