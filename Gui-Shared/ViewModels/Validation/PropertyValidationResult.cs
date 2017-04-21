/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
  * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

namespace GitHub.Shared.ViewModels.Validation
{
    public class PropertyValidationResult
    {
        /// <summary>
        /// Describes if the property passes validation
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Describes which state we are in - Valid, Not Validated, or Invalid
        /// </summary>
        public ValidationStatus Status { get; }

        /// <summary>
        /// An error message to display
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Describes if we should show this error in the UI We only show errors which have been
        /// marked specifically as Invalid and we do not show errors for inputs which have not yet
        /// been validated.
        /// </summary>
        public bool DisplayValidationError { get; }

        public static PropertyValidationResult Success { get; } = new PropertyValidationResult(ValidationStatus.Valid);

        public static PropertyValidationResult Unvalidated { get; } = new PropertyValidationResult();

        public PropertyValidationResult() : this(ValidationStatus.Unvalidated, "")
        {
        }

        public PropertyValidationResult(ValidationStatus validationStatus) : this(validationStatus, "")
        {
        }

        public PropertyValidationResult(ValidationStatus validationStatus, string message)
        {
            Status = validationStatus;
            IsValid = validationStatus == ValidationStatus.Valid;
            DisplayValidationError = validationStatus == ValidationStatus.Invalid;
            Message = message;
        }
    }
}
