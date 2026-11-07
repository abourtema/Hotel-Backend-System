using HotelResortBooking.Domain.Exceptions;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Domain.Common;

public static class DomainGuard
{
    public static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleViolationException(rule.GetType().Name, rule.Message);
        }
    }
}
