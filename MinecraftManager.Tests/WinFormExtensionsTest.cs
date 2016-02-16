using MinecraftManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Forms;

namespace MinecraftManager.Tests
{
    
    
    /// <summary>
    ///This is a test class for WinFormExtensionsTest and is intended
    ///to contain all WinFormExtensionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WinFormExtensionsTest
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
        ///A test for TryFindNode
        ///</summary>
        [TestMethod()]
        public void TryFindNode_NotFound()
        {
            TreeNode tn = new TreeNode("testRoot");
            tn.Nodes.Add("child1");

            TreeNode actual= WinFormExtensions.TryFindNode(tn, "child2",false);
            Assert.IsNull(actual);

        }

        /// <summary>
        ///A test for TryFindNode
        ///</summary>
        [TestMethod()]
        public void TryFindNode_HasMatch()
        {
            TreeNode tn = new TreeNode("testRoot");
            var expected=tn.Nodes.Add("child1");
            expected.Name="child1";

            TreeNode actual = WinFormExtensions.TryFindNode(tn, "child1", false);
            Assert.AreEqual(expected,actual);

        }

    }
}
