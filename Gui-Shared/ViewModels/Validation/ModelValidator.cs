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

using System;
using System.Linq;

namespace GitHub.Shared.ViewModels.Validation
{
    /// <summary>
    /// A validator that represents the validation state of a model. It's true if all the supplied
    /// property validators are true.
    /// </summary>
    public class ModelValidator : ViewModel
    {
        public ModelValidator(params PropertyValidator[] propertyValidators)
        {
            if (propertyValidators == null) throw new ArgumentNullException(nameof(propertyValidators));

            // Protect against mutations of the supplied array.
            var validators = propertyValidators.ToList();

            // This would be a lot cleaner with ReactiveUI but here we are.
            foreach (var validator in validators)
            {
                validator.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName != nameof(validator.ValidationResult)) return;

                    IsValid = validators.All(v => v.ValidationResult.IsValid);
                };
            }
        }

        private bool _isValid;

        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                _isValid = value;
                RaisePropertyChangedEvent(nameof(IsValid));
            }
        }
    }
}
