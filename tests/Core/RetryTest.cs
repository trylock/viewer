using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Viewer.Core;

namespace ViewerTest.Core
{
    [TestClass]
    public class RetryTest
    {
        [TestMethod]
        public async Task Retry_NoException()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    return 42;
                })
                .WithAttempts(int.MaxValue)
                .WithDelay(TimeSpan.FromMilliseconds(100))
                .When<Exception>();

            Assert.AreEqual(42, result);
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task Retry_TwoFails()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    if (count <= 2)
                        throw new IOException();
                    return 42;
                })
                .WithAttempts(int.MaxValue)
                .WithDelay(TimeSpan.FromMilliseconds(10))
                .When<IOException>();

            Assert.AreEqual(42, result);
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public async Task Retry_FinishOnLastAttempt()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    if (count <= 2)
                        throw new IOException();
                    return 42;
                })
                .WithAttempts(3)
                .WithDelay(TimeSpan.FromMilliseconds(10))
                .When<IOException>();

            Assert.AreEqual(42, result);
            Assert.AreEqual(3, count);
        }
        
        [TestMethod]
        public async Task Retry_RetryWhenTheExceptionIsAssignable()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    if (count <= 2)
                        throw new FileNotFoundException();
                    return 42;
                })
                .WithAttempts(int.MaxValue)
                .WithDelay(TimeSpan.FromMilliseconds(10))
                .When<IOException>();

            Assert.AreEqual(42, result);
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException))]
        public async Task Retry_ReachTheMaximalNumberOfAttempts()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    if (count <= 3)
                        throw new IOException();
                    return 42;
                })
                .WithAttempts(3)
                .WithDelay(TimeSpan.FromMilliseconds(10))
                .When<IOException>();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task Retry_ExceptionCannotBeAssignable()
        {
            var count = 0;
            try
            {
                await Retry.Async(() =>
                    {
                        ++count;
                        if (count <= 1)
                            throw new Exception();
                        return 42;
                    })
                    .WithAttempts(int.MaxValue)
                    .WithDelay(TimeSpan.FromMilliseconds(10))
                    .When<IOException>();
            }
            finally
            {
                Assert.AreEqual(1, count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task Retry_ExceptionHasDifferentType()
        {
            var count = 0;
            try
            {
                await Retry.Async(() =>
                    {
                        ++count;
                        if (count <= 1)
                            throw new FileNotFoundException();
                        return 42;
                    })
                    .WithAttempts(int.MaxValue)
                    .WithDelay(TimeSpan.FromMilliseconds(10))
                    .WhenExactly<IOException>();
            }
            finally
            {
                Assert.AreEqual(1, count);
            }
        }

        [TestMethod]
        public async Task Retry_CustomExceptionPredicateIsMatched()
        {
            var count = 0;
            var result = await Retry.Async(() =>
                {
                    ++count;
                    if (count <= 1)
                        throw new IOException("test");
                    return 42;
                })
                .WithAttempts(int.MaxValue)
                .WithDelay(TimeSpan.FromMilliseconds(10))
                .WhenExactly<IOException>(e => e.Message == "test");

            Assert.AreEqual(2, count);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        [ExpectedException(typeof(IOException), "test too")]
        public async Task Retry_CustomExceptionPredicateIsNotMatched()
        {
            var count = 0;
            try
            {
                await Retry.Async(() =>
                    {
                        ++count;
                        if (count <= 1)
                            throw new IOException("test too");
                        return 42;
                    })
                    .WithAttempts(int.MaxValue)
                    .WithDelay(TimeSpan.FromMilliseconds(10))
                    .WhenExactly<IOException>(e => e.Message == "test");
            }
            finally
            {
                Assert.AreEqual(1, count);
            }
        }
    }
}
