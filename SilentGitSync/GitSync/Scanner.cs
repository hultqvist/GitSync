﻿namespace SilentOrbit.GitSync;

public class Scanner
{
    readonly SyncConfig config;

    public Scanner(SyncConfig config)
    {
        this.config = config;

        config.Validate();
    }

    public void RunAllRemotes()
    {
        foreach (var remoteName in config.Remote.Keys)
            RunRemote(remoteName);
    }

    public void RunRemote(string remoteName)
    {
        foreach (var repo in config.Repo)
        {
            RunRepo(repo, remoteName);
        }
    }

    void RunRepo(RepoConfig config, string remoteName)
    {
        if (config.Remote.Contains(remoteName) == false)
            return;

        var remoteBase = new RemoteBase(this.config, remoteName);

        if (config.RecursiveOnly)
        {
            Scan((DirPath)config.Path, remoteBase, skipRoot: true);
        }
        else if (config.Recursive)
        {
            Scan((DirPath)config.Path, remoteBase);
        }
        else
        {
            //Single repo
            var gitRepo = new Repo(config.Path);
            if (gitRepo.IsWorkTree)
                return; //Skip worktrees as their main repo will be taken care of

            var remote = remoteBase.GenerateRemote(gitRepo);
            RepoSync.SyncRepo(gitRepo, remote);
        }
    }

    static void Scan(DirPath sourceBase, RemoteBase remoteBase, bool skipRoot = false)
    {
        foreach (var gitPath in ScanGit(sourceBase))
        {
            if (skipRoot)
            {
                if (gitPath.Parent == sourceBase)
                    continue;
            }

            var repoPath = gitPath.Parent;
            var source = new Repo(repoPath.Path, (repoPath - sourceBase).RelativePath);
            if (source.IsWorkTree)
                continue; //Skip worktrees as their main repo will be taken care of

            var target = remoteBase.GenerateRemote(source);
            RepoSync.SyncRepo(source, target);
        }
    }

    static IEnumerable<FullDiskPath> ScanGit(DirPath root)
    {
        var list = new List<FullDiskPath>();

        foreach (var path in root.GetDirectories(".git", SearchOption.AllDirectories))
            list.Add(path);

        foreach (var path in root.GetFiles(".git", SearchOption.AllDirectories))
            list.Add(path);

        //Reverese sort order to make sure sub repos are synchronized before the main repos are
        list.Sort();
        list.Reverse();
        return list;
    }
}
