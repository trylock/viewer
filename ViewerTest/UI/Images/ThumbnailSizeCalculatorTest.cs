using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    [TestClass]
    public class ThumbnailSizeCalculatorTest 
    {
        [TestMethod]
        public void ComputeMinimalSize_OneEntity()
        {
            var entity = new Entity("test");
            var loaderMock = new Mock<IImageLoader>();
            loaderMock.Setup(mock => mock.GetImageSize(entity)).Returns(new Size(1920, 1080));

            var calculator = new FrequentRatioThumbnailSizeCalculator(loaderMock.Object, 100);
            var size = calculator.AddEntity(entity);
            Assert.AreEqual(new Size(177, 100), size);
        }

        [TestMethod]
        public void ComputeMinimalSize_MostCommonAspectRatio()
        {
            var entity1 = new Entity("test1");
            var entity2 = new Entity("test2");
            var entity3 = new Entity("test3");
            var loaderMock = new Mock<IImageLoader>();
            loaderMock.SetupSequence(mock => mock.GetImageSize(It.IsAny<IEntity>()))
                .Returns(new Size(286, 215))
                .Returns(new Size(1920, 1080))
                .Returns(new Size(2560, 1440));

            var calculator = new FrequentRatioThumbnailSizeCalculator(loaderMock.Object, 100);
            calculator.AddEntity(entity1);
            calculator.AddEntity(entity2);
            var size = calculator.AddEntity(entity3);
            Assert.AreEqual(new Size(177, 100), size);
        }
    }
}
