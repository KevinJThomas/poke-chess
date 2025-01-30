import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import Cost from "./Cost";
import { cn, delay } from "../util";
import Damage from "./Damage";
import { useState } from "react";
import useAsyncEffect from "use-async-effect";

export default function Pokemon({
  attack,
  health,
  name,
  tier,
  id,
  cardType,
  cost,
  num,
  showTier = false,
  className,
  style,
  damage,
}) {
  const isMinion = cardType === 0;
  const [showDamage, setShowDamage] = useState(false);
  const [minionDied, setMinionDied] = useState(false);

  useAsyncEffect(async () => {
    setShowDamage(true);
    await delay(3000);
    setShowDamage(false);
  }, [damage]);

  useAsyncEffect(async () => {
    if (health <= 0 && isMinion) {
      await delay(1500);
      setMinionDied(true);
    }
  }, [health]);

  if (minionDied) {
    return;
  }

  return (
    <div className="h-20 w-20">
      <div
        id={id}
        style={{ backgroundImage: `url(/pokemon/${num}.png)`, ...style }}
        className={cn(
          "flex h-20 w-20 items-center justify-center transition-all duration-200 ease-in-out",
          isMinion && `bg-contain bg-center`,
          !isMinion && "rounded-xl bg-blue-400",
          className,
        )}
      >
        <div className="relative h-20 w-20">
          {!isMinion && <span className="text-center text-xs">{name}</span>}
          {isMinion && <Attack attack={attack} />}
          {isMinion && <Health health={health} />}
          {!!tier && showTier && <Tier tier={tier} />}
          {!isMinion && Number.isInteger(cost) && <Cost cost={cost} />}
          {!!damage && showDamage && <Damage damage={damage} />}
        </div>
      </div>
    </div>
  );
}
