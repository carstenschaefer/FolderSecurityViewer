			I do 3 calls to get all needed AD objects:
			
			
			//example textBox1: "OU=KVSW,DC=G-TAC,DC=CORP"


            results.AddRange(QueryActiveDirectory.QueryOU(this.textBox1.Text, QueryActiveDirectory.PrincialType.Ou, level));

            if (this.rbDirSearcher.Checked &&  this.cbUsers.Checked)
                results.AddRange(QueryActiveDirectory.QueryOU(this.textBox1.Text, QueryActiveDirectory.PrincialType.User, level));

            if (this.rbDirSearcher.Checked && this.cbGroups.Checked)
                results.AddRange(QueryActiveDirectory.QueryOU(this.textBox1.Text, QueryActiveDirectory.PrincialType.Group, level));