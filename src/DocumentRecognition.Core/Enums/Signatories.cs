using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentRecognition.Core.Enums
{
    public enum Signatories
    {
        /// <summary>
        /// Разраб.
        /// </summary>
        DesignedBy,

        /// <summary>
        /// Пров.
        /// </summary>
        CheckedBy,

        /// <summary>
        /// Тех. контр.
        /// </summary>
        TechnicalController,

        /// <summary>
        /// Нормоконтролер (Standards Controller)
        /// </summary>
        StandardsController,

        /// <summary>
        /// Утвердил (Approver)
        /// </summary>
        ApprovedBy,
    }
}
