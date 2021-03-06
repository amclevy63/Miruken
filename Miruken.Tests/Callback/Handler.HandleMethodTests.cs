﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Miruken.Callback;
using static Miruken.Protocol;

namespace Miruken.Tests.Callback
{
    /// <summary>
    /// Summary description for Handler
    /// </summary>
    [TestClass]
    public class HandlerHandleMethodTests
    {
        private interface IEmailFeature
        {
            int Email(string message);

            void CancelEmail(int id);
        }

        private class EmailHandler : Handler, IEmailFeature
        {
            public int Count { get; private set; }

            public int Email(string message)
            {
                return ++Count;
            }

            public void CancelEmail(int id)
            {
                var composer = id > 4
                             ? Composer.BestEffort()
                             : Composer;
                P<IBilling>(composer).Bill(4M);
            }
        }

        private interface IBilling : IResolving
        {
            decimal Bill(decimal amount);
        }

        private class Billing : IBilling
        {
            private readonly decimal _fee;

            public Billing() : this(2M)
            {            
            }

            public Billing(decimal fee)
            {
                _fee = fee;
            }

            public decimal Bill(decimal amount)
            {
                return amount + _fee;
            }
        }

        [TestMethod]
        public void Should_Handle_Methods()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Handle_Void_Methods()
        {
            var handler = new EmailHandler().Chain(new Handler(new Billing()));
            P<IEmailFeature>(handler).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Handle_Methods_Best_Effort()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.BestEffort()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Not_Propogate_Best_Effort()
        {
            var handler = new EmailHandler();
            P<IEmailFeature>(handler.BestEffort()).CancelEmail(1);
        }

        [TestMethod]
        public void Should_Apply_Nested_Best_Effort()
        {
            var handler = new EmailHandler();
            P<IEmailFeature>(handler.BestEffort()).CancelEmail(6);
        }

        [TestMethod]
        public void Should_Broadcast_Methods()
        {
            var master = new EmailHandler();
            var mirror = new EmailHandler();
            var backup = new EmailHandler();
            var email  = master.Chain(mirror, backup);
            var id     = P<IEmailFeature>(email.Broadcast()).Email("Hello");
            Assert.AreEqual(1, id);
            Assert.AreEqual(1, master.Count);
            Assert.AreEqual(1, mirror.Count);
            Assert.AreEqual(1, backup.Count);
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Methods()
        {
            var handler = new Handler();
            P<IEmailFeature>(handler).Email("Hello");
        }

        [TestMethod, ExpectedException(typeof(MissingMethodException))]
        public void Should_Reject_Unhandled_Method_Broadcast()
        {
            var handler = new Handler().Chain(new Handler());
            P<IEmailFeature>(handler).Email("Hello");
        }

        [TestMethod]
        public void Should_Ignore_Unhandled_Methods_If_Best_Effort()
        {
            var handler = new Handler();
            P<IEmailFeature>(handler.BestEffort()).Email("Hello");
        }

        [TestMethod]
        public void Should_Resolve_Methods_Inferred()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Explicitly()
        {
            var handler = new EmailHandler();
            var id      = P<IEmailFeature>(handler.Resolve()).Email("Hello");
            Assert.AreEqual(1, id);
        }

        [TestMethod]
        public void Should_Resolve_Methods_Implicitly()
        {
            var handler = new Handler(new Billing());
            var total   = P<IBilling>(handler).Bill(7.50M);
            Assert.AreEqual(9.50M, total);
        }

        [TestMethod]
        public void Should_Handle_Methods_Using_Protocol()
        {
            var billing = new Handler(new Billing(4M));
            Assert.AreEqual(7M, P<IBilling>(billing).Bill(3M));
        }
    }
}
