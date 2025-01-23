import kaplay from 'kaplay';
// import 'kaplay/global'; // uncomment if you want to use without the k. prefix

// initialize context
const k = kaplay();

// load assets
k.loadSprite('pokemon', 'sheet/pokemon.png', {
  sliceX: 12, // how many sprites are in the X axis
  sliceY: 13, // how many sprites are in the Y axis
  // anims: {
  //   crack: { from: 0, to: 3, loop: false },
  //   ghosty: { from: 4, to: 4 },
  // },
});

// k.add([
//   // list of components
//   sprite('pokemon', { frame: 0 }),
//   pos(80, 40),
//   area(),
//   body(),
// ]);

for (let i = 0; i < 10; i++) {
  k.add([
    // list of components
    sprite('pokemon', { frame: i }),
    pos(i * 128 + 128 / 2, 128),
    anchor('center'),
    area(),
    'pokemon',
  ]);
}

// foo.onClick(() => {
//   console.log('clicked', i);
// });
// }

// k.onClick('pokemon', (pokemon) => {
//   console.log('clicked', pokemon);
// });

k.onHoverUpdate('pokemon', (pokemon) => {
  if (k.isMouseDown('left')) {
    pokemon.pos = k.mousePos();
  }
  console.log('hover', pokemon);
});

// k.onHoverUpdate('pokemon', (pokemon) => {
//   console.log('hover-update', pokemon);
// });

// k.onHoverEnd('pokemon', (pokemon) => {
//   console.log('hover-end', pokemon);
// });

// k.onMouseDown('left', () => {
//   pokemon.pos = k.mousePos();
// });

// k.onMouseMove((pos) => {
//   console.log('mouse-move', pos);
// });

// k.onUpdate(() => {
//   console.log(k.isMouseDown('left'));
// });

// k.onMousePress(() => console.log('mouse-press'));

// scene('game', () => {
//   // define gravity
//   setGravity(1600);

//   // add a game object to screen
//   const player = add([
//     // list of components
//     sprite('pokemon', { frame: 0 }),
//     pos(80, 40),
//     area(),
//     body(),
//   ]);

//   // floor
//   add([
//     rect(width(), FLOOR_HEIGHT),
//     outline(4),
//     pos(0, height()),
//     anchor('botleft'),
//     area(),
//     body({ isStatic: true }),
//     color(127, 200, 255),
//   ]);

//   function jump() {
//     if (player.isGrounded()) {
//       player.jump(JUMP_FORCE);
//     }
//   }

//   // jump when user press space
//   onKeyPress('space', jump);
//   onClick(jump);

//   function spawnTree() {
//     // add tree obj
//     add([
//       rect(48, rand(32, 96)),
//       area(),
//       outline(4),
//       pos(width(), height() - FLOOR_HEIGHT),
//       anchor('botleft'),
//       color(255, 180, 255),
//       move(LEFT, SPEED),
//       'tree',
//     ]);

//     // wait a random amount of time to spawn next tree
//     wait(rand(0.5, 1.5), spawnTree);
//   }

//   // start spawning trees
//   spawnTree();

//   // lose if player collides with any game obj with tag "tree"
//   player.onCollide('tree', () => {
//     // go to "lose" scene and pass the score
//     go('lose', score);
//     burp();
//     addKaboom(player.pos);
//   });

//   // keep track of score
//   let score = 0;

//   const scoreLabel = add([text(score), pos(24, 24)]);

//   // increment score every frame
//   onUpdate(() => {
//     score++;
//     scoreLabel.text = score;
//   });
// });

// scene('lose', (score) => {
//   add([
//     sprite('bulbasaur'),
//     pos(width() / 2, height() / 2 - 80),
//     scale(2),
//     anchor('center'),
//   ]);

//   // display score
//   add([
//     text(score),
//     pos(width() / 2, height() / 2 + 80),
//     scale(2),
//     anchor('center'),
//   ]);

//   // go back to game with space is pressed
//   onKeyPress('space', () => go('game'));
//   onClick(() => go('game'));
// });

// go('game');
