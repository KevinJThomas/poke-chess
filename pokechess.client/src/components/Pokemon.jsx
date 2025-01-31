import Attack from "./Attack";
import Health from "./Health";
import Tier from "./Tier";
import Cost from "./Cost";
import { cn, delay } from "../util";
import Damage from "./Damage";
import { useState } from "react";
import useAsyncEffect from "use-async-effect";
import Tooltip from "./Tooltip";
import DivineShield from "./DivineShield";
import Venomous from "./Venomous";
import Taunt from "./Taunt";
import Stealth from "./Stealth";

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
  text,
  keywords,
}) {
  const isMinion = cardType === 0;
  const [showDamage, setShowDamage] = useState(false);
  const [minionDied, setMinionDied] = useState(false);
  const [showToolTip, setShowToolTip] = useState(false);

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

  const pokemon = (
    <div className="mx-px h-20 w-20">
      <div
        id={id}
        style={{
          backgroundImage: `url(/pokemon/${num}.png)`,
          ...style,
        }}
        className={cn(
          "-z-5 flex h-20 w-20 items-center justify-center transition-all duration-200 ease-in-out",
          isMinion && `bg-contain bg-center`,
          !isMinion && "rounded-xl bg-blue-400",
          className,
          "bg-opacity-25",
        )}
        onMouseEnter={() => !isMinion && setShowToolTip(true)}
        onMouseLeave={() => !isMinion && setShowToolTip(false)}
      >
        <div className="relative flex h-20 w-20 flex-col items-center justify-center">
          {!isMinion && (
            <span className="w-20 text-center text-xs">{name}</span>
          )}
          {isMinion && <Attack attack={attack} />}
          {isMinion && <Health health={health} />}
          {!!tier && showTier && <Tier tier={tier} />}
          {!isMinion && Number.isInteger(cost) && <Cost cost={cost} />}
          {!!damage && showDamage && <Damage damage={damage} />}
          {keywords.divineShield && <DivineShield />}
          {keywords.venomous && <Venomous />}
          {keywords.taunt && <Taunt />}
          {keywords.stealth && <Stealth />}
        </div>
      </div>
    </div>
  );

  if (isMinion) {
    return pokemon;
  }

  return (
    <Tooltip showToolTip={showToolTip} text={text}>
      {pokemon}
    </Tooltip>
  );
}
