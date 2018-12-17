using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TinkoffPayments.Tests
{
    [TestClass()]
    public class TinkoffPaymentTests
    {
        [TestMethod()]
        public void CheckPaymentStatus_TinkoffPaymentResultIsCorrect_ReturnedTinkoffPaymentResultSuccess()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            TinkoffPayment.TinkoffPaymentOperationResult result = TinkoffPayment.CheckPaymentStatus(32446948);
            
            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.Confirmed);
        }

        [TestMethod()]
        public void CheckPaymentStatus_TinkoffPaymentResultIsCorrect_ReturnedTinkoffPaymentResultRejected()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            TinkoffPayment.TinkoffPaymentOperationResult result = TinkoffPayment.CheckPaymentStatus(32447980);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.Failure);
        }

        [TestMethod()]
        public void CheckPaymentStatus_TinkoffPaymentResultIsCorrect_ReturnedTinkoffPaymentResultNew()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            TinkoffPayment.TinkoffPaymentOperationResult result = TinkoffPayment.CheckPaymentStatus(32449219);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.InProgress);
        }

        [TestMethod()]
        public void SendPayment_TinkoffPaymentStatusIsCorrect_ReturnedTinkoffPaymentResultNew()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            var result = TinkoffPayment.SendPayment("newtest", "new test method", 200);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.InProgress);
        }

        [TestMethod()]
        public void SendPayment_TinkoffPaymentStatusIsCorrect_ReturnedTinkoffPaymentResultErrorTooSmallPrice()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            var result = TinkoffPayment.SendPayment("too_small_price", "new test method", 10);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.Failure);
        }

        [TestMethod()]
        public void SendPayment_TinkoffPaymentStatusIsCorrect_ReturnedTinkoffPaymentResultErrorOrderIdNull()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            var result = TinkoffPayment.SendPayment(null, "new test method", 200);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.Failure);
        }

        [TestMethod()]
        public void SendPayment_TinkoffPaymentStatusIsCorrect_ReturnedTinkoffPaymentResultErrorAlreadyPayd()
        {
            //TinkoffPayment payment = new TinkoffPayment();

            var result = TinkoffPayment.SendPayment("~success", "new test method", 200);

            Assert.AreEqual(result.PaymentResult, TinkoffPaymentResult.Failure);
        }
    }
}