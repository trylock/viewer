using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.UI.Errors;
using Viewer.UI.QueryEditor;

namespace ViewerTest.UI.QueryEditor
{
    [TestClass]
    public class QueryErrorListenerTest
    {
        private Mock<IErrorList> _errorList;
        private QueryErrorListener _errorListener;

        [TestInitialize]
        public void Setup()
        {
            _errorList = new Mock<IErrorList>();
            _errorListener = new QueryErrorListener(_errorList.Object);
        }

        [TestMethod]
        public void OnRuntimeError_OnlyAddAnErrorOnce()
        {
            Parallel.For(0, 2000, _ =>
            {
                _errorListener.OnRuntimeError(1, 3, "Error");
            });

            _errorList.Verify(mock => mock.Add(It.Is<ErrorListEntry>(entry => 
                entry.Type == LogType.Warning &&
                entry.Line == 1 &&
                entry.Column == 3 &&
                entry.Message == "Error"
            )), Times.Once);
        }
    }
}
