# Working with changes

## Commit changes

All changes made to a repository will show up under the **Changes** view.

1. Select the changes to be committed. Can choose the All/None options, or select directories or files individually.
2. Enter a Commit summary which describes the purpose of the commit. An optional Commit description can also be entered.
3. Click the button `Commit to [branch name]`.
<img src="images/changes-view.png" alt="Changes view"/>

The commit will not be shown under the **History** view. On the top bar the button `Push (1)` indicates that there is 1 commit to push.

<img src="images/post-commit-view.png" alt="Post commit view"/>

## Push changes to GitHub

1. Click `Push` once ready to push a commit to GitHub.
<img src="images/push-view.png" alt="Push view"/>

2. A dialog will appear asking `Would you like to push changes to remote 'branch name'?` Select `Push`.
<img src="images/confirm-push-changes.png" alt="Confirm push dialog"/>

3. Another dialog will appear when the push to GitHub is complete saying `Branch pushed`. Select `ok`.
<img src="images/success-push-changes.png" alt="Branch pushed"/>

##  Revert changes

1. From the **History** view, right-click on a commit in the commit list. A `Revert` option will appear.
2. Click `Revert`.
<img src="images/revert.png" alt="Revert"/>

3. A dialog will appear asking `Are you sure you want to revert the following commit: "commit message"?`. Select `Revert`.
<img src="images/confirm-revert.png" alt="Confirm revert dialog"/>

4. A new commit appears titled `Revert "commit summary"` and the view indicates that there is 1 commit to push.
<img src="images/revert-commit.png" alt="Revert commit"/>

5. Follow the steps to push the reverted commit to GitHub.

## Pulling changes

1. Click the `Fetch` button to get all the latest branches and tags for the repository. The `Pull` button will then show the number of commits to pull from GitHub.
2. Click `Pull`.
<img src="images/pull-view.png" alt="Pull changes"/>

3. A dialog will appear asking `Would you like to pull changes from remote 'branch name'?`. Select `Pull`.
<img src="images/confirm-pull-changes.png" alt="Confirm pull changes dialog"/>

4. Another dialog appears saying `Local branch is up to date with 'branch name'`. Select `ok`. 
<img src="images/success-pull-changes.png" alt="Changes pulled"/>


