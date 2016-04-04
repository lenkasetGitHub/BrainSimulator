﻿using System;
using System.Collections.Generic;
using GoodAI.ToyWorld.Control;
using World.GameActors.GameObjects;

namespace Game
{
    class AvatarController : IAvatarController
    {
        private Avatar m_avatar;

        public AvatarController(Avatar avatar)
        {
            m_avatar = avatar;
        }

        public void SetAction(AvatarAction<object> action)
        {
            m_avatar.AddAction(action);
        }

        public IStats GetStats()
        {
            throw new NotImplementedException();
        }

        public string GetComment()
        {
            throw new NotImplementedException();
        }

        internal void ResetControls()
        {
            m_avatar.ClearConstrols();
        }
    }
}
