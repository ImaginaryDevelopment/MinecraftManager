using MinecraftManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MinecraftManager.Tests
{
    
    
    /// <summary>
    ///This is a test class for StringExtensionsTest and is intended
    ///to contain all StringExtensionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class StringExtensionsTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        ///A test for IsNullOrEmpty
        ///</summary>
        [TestMethod()]
        public void IsNullOrEmpty_empty()
        {
            string s = string.Empty;
            Assert.IsTrue(StringExtensions.IsNullOrEmpty(s));

        }

        /// <summary>
        ///A test for IsNullOrEmpty
        ///</summary>
        [TestMethod()]
        public void IsNullOrEmpty_value()
        {
            string s = "notempty";
            Assert.IsFalse(StringExtensions.IsNullOrEmpty(s));

        }
        /// <summary>
        ///A test for IsNullOrEmpty
        ///</summary>
        [TestMethod()]
        public void IsNullOrEmpty_null()
        {
            string s = null;
            Assert.IsTrue(StringExtensions.IsNullOrEmpty(s));
            
        }

        /// <summary>
        ///A test for SubstringBefore
        ///</summary>
        [TestMethod()]
        public void SubstringBeforeTest()
        {
            string indexText = "1";
            string expected = "test";
            string s =expected+ indexText ; 
            var actual = StringExtensions.SubstringBefore(s, indexText);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for SubstringAfter
        ///</summary>
        [TestMethod()]
        public void SubstringAfterTest()
        {
            
            string indexText = "1";
            string expected = "test"; 
            string s = indexText +expected ; 
            
            string actual= StringExtensions.SubstringAfter(s, indexText);
            Assert.AreEqual(expected, actual);
            
        }

        /// <summary>
        ///A test for HasValue
        ///</summary>
        [TestMethod()]
        public void HasValue_value()
        {
            string s = "aValue";
            Assert.IsTrue(StringExtensions.HasValue(s));
        }

        /// <summary>
        ///A test for HasValue
        ///</summary>
        [TestMethod()]
        public void HasValue_null()
        {
            string s = null;
            Assert.IsFalse(StringExtensions.HasValue(s));
        }
        /// <summary>
        ///A test for HasValue
        ///</summary>
        [TestMethod()]
        public void HasValue_empty()
        {
            string s = string.Empty;
            Assert.IsFalse(StringExtensions.HasValue(s));
        }
    }
}
