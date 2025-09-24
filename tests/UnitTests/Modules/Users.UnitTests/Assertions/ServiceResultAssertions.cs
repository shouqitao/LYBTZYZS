using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using LYBT.Shared.Models.Contracts.Common;
using System;

namespace LYBT.Module.Users.Tests.Assertions
{
    /// <summary>
    /// ServiceResult断言扩展
    /// </summary>
    public static class ServiceResultAssertionsExtensions
    {
        public static ServiceResultAssertions<T> Should<T>(this ServiceResult<T> instance)
        {
            return new ServiceResultAssertions<T>(instance);
        }
    }

    /// <summary>
    /// ServiceResult<T>的自定义断言
    /// </summary>
    public class ServiceResultAssertions<T> : ReferenceTypeAssertions<ServiceResult<T>, ServiceResultAssertions<T>>
    {
        public ServiceResultAssertions(ServiceResult<T> instance)
            : base(instance)
        {
        }

        protected override string Identifier => "ServiceResult";

        /// <summary>
        /// 断言操作成功
        /// </summary>
        public AndConstraint<ServiceResultAssertions<T>> BeSuccess(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result != null)
                .FailWith("Expected {context:ServiceResult} to be successful{reason}, but it was <null>.")
                .Then
                .ForCondition(result => result!.IsSuccess)
                .FailWith("Expected {context:ServiceResult} to be successful{reason}, but IsSuccess was {0}.", 
                    result => result.IsSuccess)
                .Then
                .ForCondition(result => result.Data != null)
                .FailWith("Expected {context:ServiceResult} to have data{reason}, but Data was <null>.")
                .Then
                .ForCondition(result => string.IsNullOrEmpty(result.ErrorMessage))
                .FailWith("Expected {context:ServiceResult} to have no error message{reason}, but ErrorMessage was {0}.", 
                    result => result.ErrorMessage);

            return new AndConstraint<ServiceResultAssertions<T>>(this);
        }

        /// <summary>
        /// 断言操作失败
        /// </summary>
        public AndConstraint<ServiceResultAssertions<T>> BeFailure(string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result != null)
                .FailWith("Expected {context:ServiceResult} to be failure{reason}, but it was <null>.")
                .Then
                .ForCondition(result => !result!.IsSuccess)
                .FailWith("Expected {context:ServiceResult} to be failure{reason}, but IsSuccess was {0}.", 
                    result => result.IsSuccess)
                .Then
                .ForCondition(result => !string.IsNullOrEmpty(result.ErrorMessage))
                .FailWith("Expected {context:ServiceResult} to have error message{reason}, but ErrorMessage was empty.");

            return new AndConstraint<ServiceResultAssertions<T>>(this);
        }

        /// <summary>
        /// 断言包含特定的错误消息
        /// </summary>
        public AndConstraint<ServiceResultAssertions<T>> HaveErrorMessage(string expectedMessage, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result != null && result.ErrorMessage == expectedMessage)
                .FailWith("Expected {context:ServiceResult} to have error message {0}{reason}, but it was {1}.",
                    expectedMessage, Subject?.ErrorMessage ?? "<null>");

            return new AndConstraint<ServiceResultAssertions<T>>(this);
        }

        /// <summary>
        /// 断言错误消息包含特定文本
        /// </summary>
        public AndConstraint<ServiceResultAssertions<T>> HaveErrorMessageContaining(string expectedText, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result != null && 
                    !string.IsNullOrEmpty(result.ErrorMessage) && 
                    result.ErrorMessage.Contains(expectedText))
                .FailWith("Expected {context:ServiceResult} error message to contain {0}{reason}, but it was {1}.",
                    expectedText, Subject?.ErrorMessage ?? "<null>");

            return new AndConstraint<ServiceResultAssertions<T>>(this);
        }

        /// <summary>
        /// 断言返回的数据满足条件
        /// </summary>
        public AndConstraint<ServiceResultAssertions<T>> HaveDataMatching(Func<T, bool> predicate, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result != null && result.IsSuccess && result.Data != null)
                .FailWith("Expected {context:ServiceResult} to be successful with data{reason}, but it was not.")
                .Then
                .ForCondition(result => predicate(result.Data!))
                .FailWith("Expected {context:ServiceResult} data to match the predicate{reason}, but it did not.");

            return new AndConstraint<ServiceResultAssertions<T>>(this);
        }


    }

    /// <summary>
    /// 便捷断言方法
    /// </summary>
    public static class ServiceResultAssertions
    {
        /// <summary>
        /// 断言ServiceResult成功并返回数据
        /// </summary>
        public static T AssertSuccessWithData<T>(ServiceResult<T> result, string because = "")
        {
            result.Should().BeSuccess(because);
            return result.Data!;
        }

        /// <summary>
        /// 断言ServiceResult失败并返回错误消息
        /// </summary>
        public static string AssertFailureWithMessage<T>(ServiceResult<T> result, string because = "")
        {
            result.Should().BeFailure(because);
            return result.ErrorMessage!;
        }
    }
}