﻿using Fushigi.course;
using Fushigi.ui.undo;
using Fushigi.util;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Fushigi.ui
{
    class CourseAreaEditContext(CourseArea area) : EditContextBase
    {
        public List<CourseLink> GetLinks()
        {
            return area.mLinkHolder.GetLinks();
        }

        public void AddActor(CourseActor actor)
        {
            CommitAction(area.mActorHolder.GetActors()
                .RevertableAdd(actor, $"{IconUtil.ICON_PLUS_CIRCLE} Add {actor.mActorName}"));
        }

        public void SetActorName(CourseActor actor, string newName)
        {
            actor.mActorName = newName;
        }

        public void SetObjectName(CourseActor actor, string newName)
        {
            actor.mName = newName;
        }
        public void DeleteActor(CourseActor actor)
        {
            var batchAction = BeginBatchAction();

            Console.WriteLine($"Deleting actor {actor.mActorName} [{actor.GetHash()}]");
            Deselect(actor);
            DeleteActorFromAllGroups(actor.GetHash());
            DeleteLinksWithSrcHash(actor.GetHash());
            DeleteLinksWithDestHash(actor.GetHash());
            DeleteRail(actor.GetHash());
            CommitAction(area.mActorHolder.GetActors()
                .RevertableRemove(actor));

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete {actor.mActorName}");
        }

        public void DeleteSelectedActors()
        {
            var selectedActors = GetSelectedObjects<CourseActor>().ToList();

            var batchAction = BeginBatchAction();

            foreach (var actor in selectedActors)
            {
                DeleteActor(actor);
            }

            batchAction.Commit($"{IconUtil.ICON_TRASH} Delete selected");
        }

        private void DeleteActorFromAllGroups(ulong hash)
        {
            Console.WriteLine($"Deleting actor with {hash} from groups.");
            foreach (var group in area.mGroups.GetGroupsContaining(hash))
                DeleteActorFromGroup(group, hash);
        }

        public void DeleteActorFromGroup(CourseGroup group, ulong hash)
        {
            if (group.TryGetIndexOfActor(hash, out int index))
            {
                CommitAction(
                        group.GetActors().RevertableRemoveAt(index,
                        $"Remove actor {hash} from group")
                    );
            }
        }

        private void DeleteLinksWithDestHash(ulong hash)
        {
            foreach (var index in area.mLinkHolder.GetIndicesOfLinksWithDest_ForDelete(hash))
                DeleteLinkByIndex(index);
        }

        private void DeleteLinksWithSrcHash(ulong hash)
        {
            foreach (var index in area.mLinkHolder.GetIndicesOfLinksWithSrc_ForDelete(hash))
                DeleteLinkByIndex(index);
        }

        public void DeleteLink(string name, ulong src, ulong dest)
        {
            if (area.mLinkHolder.TryGetIndexOfLink(name, src, dest, out int index))
                DeleteLinkByIndex(index);
        }

        private void DeleteLinkByIndex(int index)
        {
            var name = area.mLinkHolder.GetLinks()[index].GetLinkName();
            CommitAction(
                area.mLinkHolder.GetLinks().RevertableRemoveAt(index, $"{IconUtil.ICON_TRASH} Delete {name} Link")
            );
        }

        public bool IsActorDestForLink(CourseActor actor)
        {
            return area.mLinkHolder.GetLinkWithDestHash(actor.GetHash()) != null;
        }

        public void AddLink(CourseLink link)
        {
            Console.WriteLine($"Adding Link: Source: {link.GetSrcHash()} -- Dest: {link.GetDestHash()}");
            CommitAction(
                area.mLinkHolder.GetLinks().RevertableAdd(link,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add {link.GetLinkName()} Link")
            );
        }

        public void AddBgUnit(CourseUnit unit)
        {
            Console.WriteLine("Adding Course Unit");
            CommitAction(area.mUnitHolder.mUnits.RevertableAdd(unit,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add Tile Unit"));
        }

        public void DeleteBgUnit(CourseUnit unit)
        {
            Console.WriteLine("Deleting Course Unit");
            CommitAction(area.mUnitHolder.mUnits.RevertableRemove(unit,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Delete Tile Unit"));
        }

        public void AddWall(CourseUnit unit, Wall wall)
        {
            Console.WriteLine("Adding Wall");
            CommitAction(unit.Walls.RevertableAdd(wall,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Add Wall"));
        }

        public void DeleteWall(CourseUnit unit, Wall wall)
        {
            Console.WriteLine("Deleting Wall");
            CommitAction(unit.Walls.RevertableRemove(wall,
                    $"{IconUtil.ICON_PLUS_CIRCLE} Delete Wall"));
        }

        public CourseActorHolder GetActorHolder()
        {
            return area.mActorHolder;
        }

        public void DeleteRail(ulong hash)
        {
            Console.WriteLine($"Removing Rail attached to {hash}");
            area.mRailLinks.RemoveLinkFromSrc(hash);
        }
    }
}
