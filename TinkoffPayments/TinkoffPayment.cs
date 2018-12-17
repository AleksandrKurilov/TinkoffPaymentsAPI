using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

//using System.Configuration;

namespace TinkoffPayments
{

    /// <summary>
    /// Результат оплаты: InProgress - Еще в процессе оплаты, Confirmed - Денежные средства списаны, Failure - Отклонен, Error - Ошибка
    /// </summary>
    public enum TinkoffPaymentResult
    {
        InProgress = 0,
        Confirmed = 1,
        Failure = 2,
        Error = 3
    }

    public static class TinkoffPayment
    {
        private static readonly string TerminalKeyCode = ConfigurationManager.AppSettings["TerminalKey"];
        private static readonly string BaseUrl = ConfigurationManager.AppSettings["BaseUrl"];
        private static readonly string ContentType = ConfigurationManager.AppSettings["ContentType"];
        private static readonly string Password = ConfigurationManager.AppSettings["Password"];

        private const string TinkoffMethodInitName = "Init";
        private const string TinkoffMethodGetStateName = "GetState";


        /// <summary>
        /// Рузультат оплаты
        /// </summary>
        public class TinkoffPaymentOperationResult
        {
            public TinkoffPaymentOperationResult(TinkoffPaymentResult paymentResult, string messages, int paymentId)
            {
                PaymentResult = paymentResult;
                Messages = messages;
                PaymentId = paymentId;
            }

            public TinkoffPaymentOperationResult(TinkoffPaymentResult paymentResult, string message)
            {
                PaymentResult = paymentResult;
                Messages = message;
                PaymentId = 0;
            }

            public TinkoffPaymentResult PaymentResult { get; }
            public string Messages { get; }
            public int PaymentId { get; }
        }

        [DataContract]
        internal enum TinkoffStatus
        {
            New,
            Canceled,
            Refunded,
            [EnumMember(Value = "Deadline_Expired")]
            DeadlineExpired,
            Rejected,
            [EnumMember(Value = "Auth_Fail")]
            AuthFail,
            Confirmed
        }

        /// <summary>
        /// Класс описывает структуру данных которые возвращает метод Init 
        /// </summary>
        internal class InitRequestJsonResult
        {
            public InitRequestJsonResult(string terminalKey, string details, int paymentId, int amount, string orderId, bool success, string message, string errorCode, TinkoffStatus status, string paymentUrl)
            {
                TerminalKey = terminalKey;
                Amount = amount;
                OrderId = orderId;
                Success = success;
                Message = message;
                ErrorCode = errorCode;
                Status = status;
                PaymentUrl = paymentUrl;
                PaymentId = paymentId;
                Details = details;
            }

            public readonly string TerminalKey;
            public readonly int Amount;
            public readonly string OrderId;
            [JsonProperty("PaymentURL")]
            public readonly string PaymentUrl;
            public readonly bool Success;
            public readonly TinkoffStatus Status;
            public readonly int PaymentId;
            public readonly string ErrorCode;
            public readonly string Message;
            public readonly string Details;
        }

        /// <summary>
        /// Данные для создания заказа
        /// </summary>
        internal class SendPaymentJsonInitDataSet
        {
            public SendPaymentJsonInitDataSet(string terminalKey, string amount, string orderId, string description, string email, string successUrl, string failUrl)
            {
                TerminalKey = terminalKey;
                Amount = amount;
                OrderId = orderId;
                Description = description;
                SuccessUrl = successUrl;
                FailUrl = failUrl;

                Data = new DataModel()
                {
                    Email = email
                };
            }

            public readonly string TerminalKey;
            public readonly string Amount;
            public readonly string OrderId;
            public readonly string Description;
            [JsonProperty("SuccessURL")]
            public string SuccessUrl;
            [JsonProperty("FailURL")]
            public string FailUrl;
            [JsonProperty("DATA")]
            public readonly DataModel Data;
        }

        internal class DataModel
        {
            public string Email { get; set; }
        }
        
        /// <summary>
        /// Данные для проверки статуса платежа
        /// </summary>
        internal class CheckPaymentJsonInitDataSet
        {
            public CheckPaymentJsonInitDataSet(string terminalKey, int paymentId, string token)
            {
                TerminalKey = terminalKey;
                PaymentId = paymentId;
                Token = token;
            }
            public readonly string TerminalKey;
            public readonly int PaymentId;
            public readonly string Token;
        }

        /// <summary>
        ///  Инициирует платёжную сессию
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="description"></param>
        /// <param name="amount"></param>
        /// <param name="email"></param>
        /// <param name="successUrl"></param>
        /// <param name="failUrl"></param>
        /// <returns></returns>
        public static TinkoffPaymentOperationResult SendPayment(string orderId, string description, int amount, string email, string successUrl, string failUrl)
        {
            var byteArray = GetDataBytes(orderId, description, amount, email, successUrl, failUrl);
            string response;
            try
            {
                response = GetResponse(TinkoffMethodInitName, byteArray);
            }
            catch (Exception e)
            {
                return new TinkoffPaymentOperationResult(TinkoffPaymentResult.Error, $"{e.Message}");
            }

            InitRequestJsonResult initResult = JsonConvert.DeserializeObject<InitRequestJsonResult>(response);

            if (initResult.Success)
                return new TinkoffPaymentOperationResult(TinkoffStatusToTinkoffPaymentResult(initResult.Status), initResult.PaymentUrl, initResult.PaymentId);

            return new TinkoffPaymentOperationResult(TinkoffPaymentResult.Failure, $"ErrorCode:{initResult.ErrorCode}, Details:{initResult.Details}, Message:{initResult.Message}");
        }

        private static TinkoffPaymentResult TinkoffStatusToTinkoffPaymentResult(TinkoffStatus status)
        {
            TinkoffPaymentResult state;
            switch (status)
            {
                case TinkoffStatus.New:
                    state = TinkoffPaymentResult.InProgress;
                    break;
                case TinkoffStatus.Canceled://Платёж отменен Продавцом
                    state = TinkoffPaymentResult.Failure;
                    break;
                case TinkoffStatus.Refunded://Произведен возврат денежных средств
                    state = TinkoffPaymentResult.Failure;
                    break;
                case TinkoffStatus.DeadlineExpired://Истёк срок оплаты сессии
                    state = TinkoffPaymentResult.Failure;
                    break;
                case TinkoffStatus.Rejected://Платёж отклонен Банком
                    state = TinkoffPaymentResult.Failure;
                    break;
                case TinkoffStatus.AuthFail://Неуспешная попытка оплаты в ACQ
                    state = TinkoffPaymentResult.Failure;
                    break;
                case TinkoffStatus.Confirmed://Платеж успешный
                    state = TinkoffPaymentResult.Confirmed;
                    break;
                default:
                    state = TinkoffPaymentResult.InProgress;
                    break;
            }
            return state;
        }

        /// <summary>
        /// Возвращает текуший статус платежа
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public static TinkoffPaymentOperationResult CheckPaymentStatus(int paymentId)
        {
            var byteArray = GetDataBytes(paymentId);
            string response;
            try
            {
                response = GetResponse(TinkoffMethodGetStateName, byteArray);
            }
            catch (Exception e)
            {
                return new TinkoffPaymentOperationResult(TinkoffPaymentResult.Error, $"{e.Message}");
            }

            InitRequestJsonResult initResult = JsonConvert.DeserializeObject<InitRequestJsonResult>(response);

            if (initResult.Success)
                return new TinkoffPaymentOperationResult(TinkoffStatusToTinkoffPaymentResult(initResult.Status), initResult.Message);

            return new TinkoffPaymentOperationResult(TinkoffPaymentResult.Failure,
                $"ErrorCode:{initResult.ErrorCode}, Details:{initResult.Details}, Message:{initResult.Message}");
        }

        /// <summary>
        /// Отправляет запрос к серверу и возвращает строку response
        /// </summary>
        /// <param name="method">Метод API который вызываем</param>
        /// <param name="dataArray">Массив данных которые передаем</param>
        /// <returns></returns>
        private static string GetResponse(string method, byte[] dataArray)
        {
            var request = InitRequest(method);
            request.ContentLength = dataArray.Length;

            using (var dataStream = request.GetRequestStream())
                dataStream.Write(dataArray, 0, dataArray.Length);


            WebResponse response = request.GetResponse();

            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null) return "";

                using (StreamReader reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Инициализирует новый объект WebRequest
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private static WebRequest InitRequest(string method)
        {
            var request = WebRequest.Create(BaseUrl + method);
            request.Method = "POST";
            request.ContentType = ContentType;
            return request;
        }

        /// <summary>
        /// Преобразует набор данных в массив байтов для отправки на сервер
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="description"></param>
        /// <param name="amount"></param>
        /// <param name="email"></param>
        /// <param name="successUrl"></param>
        /// <param name="failUrl"></param>
        /// <returns></returns>
        private static Byte[] GetDataBytes(string orderId, string description, int amount, string email, string successUrl, string failUrl)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                new SendPaymentJsonInitDataSet(TerminalKeyCode,
                    amount.ToString(),
                    orderId,
                    description,
                    email,
                    successUrl,
                    failUrl
                )));
        }

        /// <summary>
        /// Преобразует набор данных в массив байтов для отправки на сервер
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        private static Byte[] GetDataBytes(int paymentId)
        {
            string token = GetToken(Password + paymentId + TerminalKeyCode);

            return Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new CheckPaymentJsonInitDataSet(TerminalKeyCode, paymentId, token)));
        }

        /// <summary>
        /// Вычисляет токен для запроса
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string GetToken(string data)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(data));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }
}
