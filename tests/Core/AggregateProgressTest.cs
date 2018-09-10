using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Core;

namespace ViewerTest.Core
{
    [TestClass]
    public class AggregateProgressTest
    {
        [TestMethod]
        public void Report_EmptyProgress()
        {
            var progress = AggregateProgress.CreateEmpty<int>();
            progress.Report(1);
            progress.Report(2);
        }

        [TestMethod]
        public void Report_SingleProgress()
        {
            var progress = new Mock<IProgress<int>>();
            var aggregate = AggregateProgress.Create(progress.Object);

            aggregate.Report(1);
            aggregate.Report(2);

            progress.Verify(mock => mock.Report(1), Times.Once);
            progress.Verify(mock => mock.Report(2), Times.Once);
            progress.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Report_MultipleProgressObjects()
        {
            var progress = new[]
            {
                new Mock<IProgress<int>>(),
                new Mock<IProgress<int>>(),
                new Mock<IProgress<int>>(),
            };
            var aggregate = AggregateProgress.Create(progress.Select(item => item.Object).ToArray());

            aggregate.Report(1);
            aggregate.Report(2);

            foreach (var item in progress)
            {
                item.Verify(mock => mock.Report(1), Times.Once);
                item.Verify(mock => mock.Report(2), Times.Once);
                item.VerifyNoOtherCalls();
            }
        }
    }
}
