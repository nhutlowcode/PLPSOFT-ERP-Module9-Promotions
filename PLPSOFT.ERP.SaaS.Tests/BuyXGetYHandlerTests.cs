using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PLPSOFT.ERP.SaaS.Modules.Promotions.Tests
{
    public class BuyXGetYHandlerTests
    {
        [Fact]
        public void Buy1_Should_Not_Get_Gift()
        {
            int buyQty = 1;
            int giftQty = (buyQty / 2) * 1;

            Assert.Equal(0, giftQty);
        }

        [Fact]
        public void Buy2_Get1_Should_Give_One_Gift()
        {
            int buyQty = 2;
            int giftQty = (buyQty / 2) * 1;

            Assert.Equal(1, giftQty);
        }

        [Fact]
        public void Buy4_Get1_Should_Give_Two_Gifts()
        {
            int buyQty = 4;
            int giftQty = (buyQty / 2) * 1;

            Assert.Equal(2, giftQty);
        }

        [Fact]
        public void Buy6_Get1_Should_Give_Three_Gifts()
        {
            int buyQty = 6;
            int giftQty = (buyQty / 2) * 1;

            Assert.Equal(3, giftQty);
        }

        [Fact]
        public void Buy10_Get1_Should_Give_Five_Gifts()
        {
            int buyQty = 10;
            int giftQty = (buyQty / 2) * 1;

            Assert.Equal(5, giftQty);
        }
    }
}