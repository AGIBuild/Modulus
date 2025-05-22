using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Modulus.App.Options
{
    public class PluginOptionsValidation : IValidateOptions<PluginOptions>
    {
        public ValidateOptionsResult Validate(string? name, PluginOptions options)
        {
            try
            {
                // First validate required fields using data annotations
                var validationContext = new ValidationContext(options);
                Validator.ValidateObject(options, validationContext, validateAllProperties: true);

                // Expand environment variables in paths
                var installPath = Environment.ExpandEnvironmentVariables(options.InstallPath);
                var userPath = Environment.ExpandEnvironmentVariables(options.UserPath);

                // Create directories if they don't exist
                if (!Directory.Exists(installPath))
                {
                    Directory.CreateDirectory(installPath);
                }

                if (!Directory.Exists(userPath))
                {
                    Directory.CreateDirectory(userPath);
                }

                // Validate that paths are now accessible
                if (!Directory.Exists(installPath))
                {
                    return ValidateOptionsResult.Fail($"Unable to create or access plugin install directory '{installPath}'");
                }

                if (!Directory.Exists(userPath))
                {
                    return ValidateOptionsResult.Fail($"Unable to create or access plugin user directory '{userPath}'");
                }

                return ValidateOptionsResult.Success;
            }
            catch (ValidationException ex)
            {
                return ValidateOptionsResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return ValidateOptionsResult.Fail($"Plugin directory validation failed: {ex.Message}");
            }
        }
    }
}
