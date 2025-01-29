import Pokemon from "./Pokemon";

export default function OpponentPokemon({ opponent }) {
  return (
    <div>
      {opponent.board.map((pokemon, index) => (
        <Pokemon key={pokemon.id} {...pokemon} />
      ))}
    </div>
  );
}
