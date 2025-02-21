import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";
import clsx from "clsx";
import useAsyncEffect from "use-async-effect";
import { useState } from "react";
import { delay } from "../util";
import HeroDamage from "./HeroDamage";
import OpponentTooltip from "./OpponentTooltip";
import { Droppable } from "@hello-pangea/dnd";

export default function Hero({
  hero,
  health,
  name,
  armor,
  tier,
  className,
  id,
  style,
  damage,
  type,
  combatHistory,
  winStreak,
  isDropDisabled,
}) {
  const [showDamage, setShowDamage] = useState(false);
  const [showTooltip, setShowTooltip] = useState(false);

  function getListStyle(isDraggingOver) {
    return {
      // background: isDraggingOver ? "lightblue" : "lightgrey",
      // display: "flex",
      // padding: grid,
      // width: itemsLength * 68.44 + 16,
    };
  }

  useAsyncEffect(async () => {
    setShowDamage(true);
    await delay(3000);
    setShowDamage(false);
  }, [damage]);

  const heroPortrait = (
    <div
      id={id}
      className={clsx(
        "relative flex h-24 w-24 items-center justify-center truncate rounded-xl bg-red-300 outline-2 outline-red-900 transition-all duration-400 ease-in-out",
        className,
      )}
      style={style}
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <div className="flex flex-col items-center">
        {hero && (
          <div className="font-medium">{hero.name ?? "Unknown Hero"}</div>
        )}
        <div className="text-xs">{name}</div>
      </div>
      {!!health && <Health health={health} />}
      {!!armor && <Armor armor={armor} />}
      {!!tier && <Tier tier={tier} />}
      {!!damage && showDamage && <HeroDamage damage={damage} />}
    </div>
  );

  if (type === "opponents") {
    return (
      <OpponentTooltip
        combatHistory={combatHistory}
        showToolTip={showTooltip}
        winStreak={winStreak}
      >
        {heroPortrait}
      </OpponentTooltip>
    );
  }

  if (type === "player-shop") {
    return (
      <Droppable
        droppableId="droppable-hero"
        direction="vertical"
        isDropDisabled={isDropDisabled}
      >
        {(provided, snapshot) => (
          <>
            <div
              ref={provided.innerRef}
              style={getListStyle(snapshot.isDraggingOver)}
              {...provided.droppableProps}
            >
              {heroPortrait}
            </div>
            <span className="hidden">{provided.placeholder}</span>
          </>
        )}
      </Droppable>
    );
  }

  return heroPortrait;
}
