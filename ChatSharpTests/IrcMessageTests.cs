using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatSharp.Tests
{
    [TestClass]
    public class IrcMessageTests
    {
        [TestMethod]
        public void NewValidMessage()
        {
            try
            {
                _ = new IrcMessage(@":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            }
            catch (Exception e)
            {
                Assert.Fail("Expected no exception, got: {0}", e.Message);
            }
        }

        [TestMethod]
        public void NewValidMessage_Command()
        {
            IrcMessage fromMessage = new(@":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            Assert.AreEqual(fromMessage.Command, "PRIVMSG");
        }

        [TestMethod]
        public void NewValidMessage_Prefix()
        {
            IrcMessage fromMessage = new(@":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            Assert.AreEqual(fromMessage.Prefix, "user!~ident@host");
        }

        [TestMethod]
        public void NewValidMessage_Params()
        {
            IrcMessage fromMessage = new(@":user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            var compareParams = new[] { "target", "Lorem ipsum dolor sit amet" };
            CollectionAssert.AreEqual(fromMessage.Parameters, compareParams);
        }

        [TestMethod]
        public void NewValidMessage_Tags()
        {
            IrcMessage fromMessage =
                new("@a=123;b=456;c=789 :user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            var compareTags = new[]
            {
                new KeyValuePair<string, string>("a", "123"),
                new KeyValuePair<string, string>("b", "456"),
                new KeyValuePair<string, string>("c", "789")
            };
            CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
        }

        [TestMethod]
        public void NewValidMessage_Tags02()
        {
            IrcMessage fromMessage = new("@aaa=bbb;ccc;example.com/ddd=eee :nick!ident@host.com PRIVMSG me :Hello");
            var compareTags = new[]
            {
                new KeyValuePair<string, string>("aaa", "bbb"),
                new KeyValuePair<string, string>("ccc", null),
                new KeyValuePair<string, string>("example.com/ddd", "eee")
            };
            CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
        }

        [TestMethod]
        public void NewValidMessage_TagsWithSemicolon()
        {
            IrcMessage fromMessage =
                new(@"@a=123\:456;b=456\:789;c=789\:123 :user!~ident@host PRIVMSG target :Lorem ipsum dolor sit amet");
            var compareTags = new[]
            {
                new KeyValuePair<string, string>("a", "123;456"),
                new KeyValuePair<string, string>("b", "456;789"),
                new KeyValuePair<string, string>("c", "789;123")
            };
            CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
        }

        [TestMethod]
        public void NewValidMessage_TagsNoValue()
        {
            IrcMessage fromMessage = new("@a=;b :nick!ident@host.com PRIVMSG me :Hello");
            var compareTags = new[]
            {
                new KeyValuePair<string, string>("a", ""),
                new KeyValuePair<string, string>("b", null)
            };
            CollectionAssert.AreEqual(fromMessage.Tags, compareTags);
        }

        [TestMethod]
        public void Timestamp_CompareISOString()
        {
            IrcMessage[] messages =
            {
                new("@time=2011-10-19T16:40:51.620Z :Angel!angel@example.org PRIVMSG Wiz :Hello"),
                new("@time=2012-06-30T23:59:59.419Z :John!~john@1.2.3.4 JOIN #chan")
            };

            string[] timestamps =
            {
                "2011-10-19T16:40:51.620Z",
                "2012-06-30T23:59:59.419Z"
            };

            Assert.AreEqual(messages[0].Timestamp.ToISOString(), timestamps[0]);
            Assert.AreEqual(messages[1].Timestamp.ToISOString(), timestamps[1]);
        }

        [TestMethod]
        public void Timestamp_FromTimestamp()
        {
            IrcMessage[] messages =
            {
                new("@t=1504923966 :Angel!angel@example.org PRIVMSG Wiz :Hello"),
                new("@t=1504923972 :John!~john@1.2.3.4 JOIN #chan")
            };

            string[] timestamps =
            {
                "2017-09-09T02:26:06.000Z",
                "2017-09-09T02:26:12.000Z"
            };

            Assert.AreEqual(messages[0].Timestamp.ToISOString(), timestamps[0]);
            Assert.AreEqual(messages[1].Timestamp.ToISOString(), timestamps[1]);
        }

        [TestMethod]
        public void Timestamp_FailOnLeap()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                new IrcMessage("@time=2012-06-30T23:59:60.419Z :John!~john@1.2.3.4 JOIN #chan"));
        }
    }
}