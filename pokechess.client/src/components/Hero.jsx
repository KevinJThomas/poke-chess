import Armor from "./Armor";
import Health from "./Health";
import Tier from "./Tier";
import clsx from "clsx";
import useAsyncEffect from "use-async-effect";
import { useState } from "react";
import { delay } from "../util";
import HeroDamage from "./HeroDamage";
import OpponentTooltip from "./OpponentTooltip";

export default function Hero({
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
}) {
  const [showDamage, setShowDamage] = useState(false);
  const [showTooltip, setShowTooltip] = useState(false);

  useAsyncEffect(async () => {
    setShowDamage(true);
    await delay(3000);
    setShowDamage(false);
  }, [damage]);

  const hero = (
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
      <span>{name}</span>
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
        {hero}
      </OpponentTooltip>
    );
  }

  return hero;
}
