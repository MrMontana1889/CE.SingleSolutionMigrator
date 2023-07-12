﻿// CSharpProjectMigrator.cs
// Copyright (c) 2023 Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;

namespace CE.SingleSolutionMigrator.Migrators
{
    public class CSharpProjectMigrator : BaseProjectMigrator
    {
        #region Constructor
        public CSharpProjectMigrator(PerforceFacade p4)
            : base(p4)
        {
        }
        #endregion

        #region Public Methods
        public override void Migrate(string rootPath, string filename)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}