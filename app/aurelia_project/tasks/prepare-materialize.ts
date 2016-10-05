import * as gulp from 'gulp';
import * as changedInPlace from 'gulp-changed-in-place';
import * as project from '../aurelia.json';
import * as path from 'path';

export default function prepareMaterialize() {
  let source = 'node_modules/materialize-css';

  return gulp.src(path.join(source, 'dist/fonts/roboto/*'))
    .pipe(changedInPlace({firstPass:true}))
    .pipe(gulp.dest(path.join(project.platform.output, '../materialize-css/fonts/roboto')));
}