using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum EnumErrors
    {
        //500-599 codigos de apis
        Success = 200,
        missingName = 100,
        missingPassword = 101,
        missingEmail = 102,
        missingAdress = 103,
        InvalidEmail = 104,
        PasswordNotStrong = 105,
        GuidMissing = 106,
        ErrorActivatingUser = 107,
        missingLastName = 107,
        EmailAlreadyExists = 108,
        IncorrectEmail =109,
        wrongPassword = 110,
        ApiRequestFailed = 500,
        EmptyResponse = 501,
        InvalidJson = 502,
        ExceptionThrown = 503,
        InvalidRoute = 504,
        InvalidVehicle = 505,
        DataBaseError = 601,
    }
}
